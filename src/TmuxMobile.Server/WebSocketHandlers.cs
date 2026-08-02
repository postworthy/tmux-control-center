using System.Net.WebSockets;
using System.Security.Claims;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Server;

public sealed class WebSocketHandlers(
    IInventoryStore inventory,
    ITmuxTargetResolver targetResolver,
    ITmuxService tmux,
    IPseudoTerminalFactory ptyFactory,
    IOptions<TmuxOptions> tmuxOptions,
    IOptions<SecurityOptions> securityOptions,
    TerminalConnectionLimiter limiter,
    IAuditLogger audit,
    ILogger<WebSocketHandlers> logger)
{
    public async Task InventoryAsync(HttpContext context)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var channel = Channel.CreateBounded<InventorySnapshot>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        void Changed(InventorySnapshot snapshot) => channel.Writer.TryWrite(snapshot);
        inventory.Changed += Changed;
        channel.Writer.TryWrite(inventory.Current);
        logger.LogInformation("Inventory WebSocket connected for {User}", UserId(context.User));
        try
        {
            await foreach (var snapshot in channel.Reader.ReadAllAsync(context.RequestAborted))
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, context.RequestAborted);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex) { logger.LogDebug(ex, "Inventory WebSocket disconnected"); }
        finally
        {
            inventory.Changed -= Changed;
            channel.Writer.TryComplete();
            await CloseQuietlyAsync(socket);
            logger.LogInformation("Inventory WebSocket disconnected for {User}", UserId(context.User));
        }
    }

    public async Task TerminalAsync(HttpContext context, string sessionId)
    {
        var user = UserId(context.User);
        var security = securityOptions.Value;
        using var lease = limiter.TryAcquire(user, security.MaxTerminalConnections,
            security.MaxTerminalConnectionsPerUser);
        if (lease is null)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }
        var rawTarget = await targetResolver.ResolveRawSessionAsync(sessionId, context.RequestAborted);
        if (rawTarget is null)
        {
            await audit.WriteAsync("terminal.connect", user, sessionId, false, context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(tmuxOptions.Value.SocketName))
            args.AddRange(["-L", tmuxOptions.Value.SocketName]);
        args.AddRange(["attach-session", "-t", rawTarget]);

        IPseudoTerminal pty;
        try
        {
            pty = await ptyFactory.StartAsync(tmuxOptions.Value.ExecutablePath, args,
                new TerminalSize(80, 24), new Dictionary<string, string>
                {
                    ["TERM"] = "xterm-256color",
                    ["COLORTERM"] = "truecolor"
                }, context.RequestAborted);
        }
        catch
        {
            await audit.WriteAsync("terminal.connect", user, sessionId, false, CancellationToken.None);
            throw;
        }
        await using var ownedPty = pty;
        await audit.WriteAsync("terminal.connect", user, sessionId, true, context.RequestAborted);
        logger.LogInformation("Terminal WebSocket connected for {User} to {SessionId}; PTY child {ProcessId}",
            user, sessionId, pty.ProcessId);

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        using var idle = new CancellationTokenSource(TimeSpan.FromMinutes(security.TerminalIdleTimeoutMinutes));
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, idle.Token);
        var sendLock = new SemaphoreSlim(1, 1);
        var history = new TerminalHistoryState();
        try
        {
            var output = PumpOutputAsync(pty, socket, sendLock, combined.Token);
            var input = PumpInputAsync(pty, socket, security.MaxWebSocketMessageBytes, idle,
                TimeSpan.FromMinutes(security.TerminalIdleTimeoutMinutes), sessionId, user, history, combined.Token);
            var heartbeat = HeartbeatAsync(socket, sendLock, combined.Token);
            await Task.WhenAny(output, input, pty.WaitForExitAsync(combined.Token));
            lifetime.Cancel();
            await pty.DisposeAsync();
            try { await Task.WhenAll(output, input, heartbeat); } catch (OperationCanceledException) { }
        }
        catch (WebSocketException ex) { logger.LogDebug(ex, "Terminal WebSocket disconnected"); }
        finally
        {
            lifetime.Cancel();
            if (history.EnteredByConnection)
            {
                try
                {
                    await tmux.ControlSessionHistoryAsync(sessionId, TerminalHistoryAction.Latest, 0,
                        CancellationToken.None);
                    await audit.WriteAsync("terminal.history.cleanup", user, sessionId, true, CancellationToken.None);
                }
                catch (Exception exception) when (exception is TmuxCommandException or TmuxNotFoundException)
                {
                    logger.LogWarning(exception, "Unable to clean up terminal history mode for {SessionId}", sessionId);
                    await audit.WriteAsync("terminal.history.cleanup", user, sessionId, false, CancellationToken.None);
                }
            }
            await CloseQuietlyAsync(socket);
            await audit.WriteAsync("terminal.disconnect", user, sessionId, true, CancellationToken.None);
            logger.LogInformation("Terminal WebSocket disconnected for {User} from {SessionId}", user, sessionId);
        }
    }

    private static async Task PumpOutputAsync(
        IPseudoTerminal pty, WebSocket socket, SemaphoreSlim sendLock, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        while (!cancellationToken.IsCancellationRequested)
        {
            int read;
            try { read = await pty.Output.ReadAsync(buffer, cancellationToken); }
            catch (IOException)
            {
                // Linux PTY masters report EIO, rather than a zero-byte read, after the slave closes.
                break;
            }
            if (read == 0) break;
            await sendLock.WaitAsync(cancellationToken);
            try { await socket.SendAsync(buffer.AsMemory(0, read), WebSocketMessageType.Binary, true, cancellationToken); }
            finally { sendLock.Release(); }
        }
    }

    private async Task PumpInputAsync(
        IPseudoTerminal pty, WebSocket socket, int maxMessageBytes, CancellationTokenSource idle,
        TimeSpan idleTimeout, string sessionId, string user, TerminalHistoryState history,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[maxMessageBytes];
        var inputRate = new TerminalInputRateState(
            securityOptions.Value.MaxTerminalInputMessagesPerSecond,
            securityOptions.Value.MaxTerminalInputBytesPerSecond);
        while (!cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;
            try { result = await socket.ReceiveAsync(buffer, cancellationToken); }
            catch (Exception exception) when (exception is WebSocketException or IOException or ObjectDisposedException)
            {
                break;
            }
            if (result.MessageType == WebSocketMessageType.Close) break;
            if (!result.EndOfMessage)
            {
                await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too large", CancellationToken.None);
                break;
            }
            if (!inputRate.TryConsume(result.Count))
            {
                logger.LogWarning("Terminal input rate limit exceeded for {User} on {SessionId}", user, sessionId);
                await audit.WriteAsync("terminal.input.rate-limit", user, sessionId, false, CancellationToken.None);
                await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation,
                    "Terminal input rate limit exceeded");
                break;
            }
            idle.CancelAfter(idleTimeout);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                await pty.Input.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                await pty.Input.FlushAsync(cancellationToken);
                continue;
            }
            try
            {
                using var document = JsonDocument.Parse(buffer.AsMemory(0, result.Count));
                var root = document.RootElement;
                var type = root.GetProperty("type").GetString();
                if (type == "input")
                {
                    var data = root.GetProperty("data").GetString() ?? "";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    if (bytes.Length > maxMessageBytes) throw new InvalidDataException("Terminal input is too large.");
                    await pty.Input.WriteAsync(bytes, cancellationToken);
                    await pty.Input.FlushAsync(cancellationToken);
                }
                else if (type == "resize")
                {
                    await pty.ResizeAsync(new(root.GetProperty("cols").GetInt32(),
                        root.GetProperty("rows").GetInt32()), cancellationToken);
                }
                else if (type == "history")
                {
                    var actionName = root.GetProperty("action").GetString()
                        ?? throw new InvalidDataException("History action is required.");
                    if (!Enum.TryParse<TerminalHistoryAction>(actionName, true, out var action) ||
                        !Enum.GetNames<TerminalHistoryAction>().Any(name =>
                            string.Equals(name, actionName, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidDataException("Unsupported history action.");
                    var pages = action == TerminalHistoryAction.Latest
                        ? 0
                        : root.GetProperty("pages").GetInt32();
                    if (action != TerminalHistoryAction.Latest && pages < 1)
                        throw new InvalidDataException("History pages must be positive.");
                    pages = Math.Clamp(pages, 0, 3);

                    if (!history.TryConsumeOperation())
                    {
                        logger.LogWarning("Terminal history rate limit exceeded for {User} on {SessionId}",
                            user, sessionId);
                        await audit.WriteAsync("terminal.history.rate-limit", user, sessionId, false,
                            CancellationToken.None);
                        await CloseQuietlyAsync(socket, WebSocketCloseStatus.PolicyViolation,
                            "History rate limit exceeded");
                        break;
                    }

                    try
                    {
                        var entered = await tmux.ControlSessionHistoryAsync(sessionId, action, pages,
                            cancellationToken);
                        history.EnteredByConnection |= entered;
                        if (action == TerminalHistoryAction.Latest) history.EnteredByConnection = false;
                        await audit.WriteAsync($"terminal.history.{actionName.ToLowerInvariant()}", user,
                            sessionId, true, cancellationToken);
                    }
                    catch (Exception exception) when (exception is TmuxCommandException or TmuxNotFoundException)
                    {
                        logger.LogWarning(exception, "Terminal history operation failed for {SessionId}", sessionId);
                        await audit.WriteAsync($"terminal.history.{actionName.ToLowerInvariant()}", user,
                            sessionId, false, CancellationToken.None);
                        await CloseQuietlyAsync(socket, WebSocketCloseStatus.InternalServerError,
                            "History control failed");
                        break;
                    }
                }
                else if (type != "pong")
                {
                    throw new InvalidDataException("Unsupported terminal message.");
                }
            }
            catch (Exception exception) when (
                exception is JsonException or KeyNotFoundException or InvalidDataException or
                    ArgumentException or InvalidOperationException or FormatException or OverflowException)
            {
                await CloseQuietlyAsync(socket, WebSocketCloseStatus.InvalidPayloadData,
                    "Invalid terminal message");
                break;
            }
        }
    }

    private static async Task HeartbeatAsync(
        WebSocket socket, SemaphoreSlim sendLock, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await sendLock.WaitAsync(cancellationToken);
            try
            {
                await socket.SendAsync("""{"type":"ping"}"""u8.ToArray(),
                    WebSocketMessageType.Text, true, cancellationToken);
            }
            finally { sendLock.Release(); }
        }
    }

    private static async Task CloseQuietlyAsync(WebSocket socket,
        WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string description = "Closing")
    {
        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await socket.CloseAsync(status, description, CancellationToken.None);
        }
        catch (Exception exception) when (exception is WebSocketException or IOException or ObjectDisposedException) { }
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    private sealed class TerminalHistoryState
    {
        private const double Capacity = 4;
        private const double TokensPerSecond = 4;
        private double tokens = Capacity;
        private long lastRefill = Stopwatch.GetTimestamp();

        public bool EnteredByConnection { get; set; }

        public bool TryConsumeOperation()
        {
            var now = Stopwatch.GetTimestamp();
            tokens = Math.Min(Capacity,
                tokens + Stopwatch.GetElapsedTime(lastRefill, now).TotalSeconds * TokensPerSecond);
            lastRefill = now;
            if (tokens < 1) return false;
            tokens--;
            return true;
        }
    }

    private sealed class TerminalInputRateState(int messagesPerSecond, int bytesPerSecond)
    {
        private double messageTokens = messagesPerSecond;
        private double byteTokens = bytesPerSecond;
        private long lastRefill = Stopwatch.GetTimestamp();

        public bool TryConsume(int bytes)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(lastRefill, now).TotalSeconds;
            messageTokens = Math.Min(messagesPerSecond, messageTokens + elapsed * messagesPerSecond);
            byteTokens = Math.Min(bytesPerSecond, byteTokens + elapsed * bytesPerSecond);
            lastRefill = now;
            if (messageTokens < 1 || byteTokens < bytes) return false;
            messageTokens--;
            byteTokens -= bytes;
            return true;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
