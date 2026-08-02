using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security;
using System.Text;
using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed class InventoryStore(
    ITmuxService tmux,
    TimeProvider timeProvider,
    ILogger<InventoryStore> logger) : IInventoryStore
{
    private InventorySnapshot _current = new(0, DateTimeOffset.MinValue, []);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    public InventorySnapshot Current => Volatile.Read(ref _current);
    public event Action<InventorySnapshot>? Changed;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var sessions = await tmux.GetSessionsAsync(cancellationToken);
            var prior = Current;
            if (InventoryComparer.MeaningfullyEqual(prior.Sessions, sessions)) return;
            var next = new InventorySnapshot(prior.Version + 1, timeProvider.GetUtcNow(), sessions);
            Volatile.Write(ref _current, next);
            Changed?.Invoke(next);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Inventory refresh failed");
        }
        finally { _refreshLock.Release(); }
    }
}

public sealed class InventoryPollingService(
    IInventoryStore store,
    IOptions<TmuxOptions> options,
    ILogger<InventoryPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inventory polling started");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));
        do { await store.RefreshAsync(stoppingToken); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Inventory polling stopped");
        return base.StopAsync(cancellationToken);
    }
}

public sealed class JsonLineAuditLogger(
    IOptions<AuditOptions> options,
    TimeProvider timeProvider,
    IHostEnvironment environment,
    ILogger<JsonLineAuditLogger> logger) : IAuditLogger
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> WriteAsync(string action, string subject, string target, bool succeeded,
        CancellationToken cancellationToken)
    {
        var path = Path.IsPathFullyQualified(options.Value.Destination)
            ? options.Value.Destination
            : Path.Combine(environment.ContentRootPath, options.Value.Destination);
        var line = JsonSerializer.Serialize(new
        {
            timestamp = timeProvider.GetUtcNow(),
            action,
            subject,
            target,
            succeeded
        }) + Environment.NewLine;
        try
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                AuditStorage.Prepare(path);
                var streamOptions = new FileStreamOptions
                {
                    Mode = FileMode.Append,
                    Access = FileAccess.Write,
                    Share = FileShare.Read,
                    Options = FileOptions.Asynchronous
                };
                if (OperatingSystem.IsLinux())
                    streamOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                await using var stream = new FileStream(path, streamOptions);
                var bytes = Encoding.UTF8.GetBytes(line);
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                return true;
            }
            finally { _gate.Release(); }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(exception, "Audit write canceled for action {Action} and target {Target}", action, target);
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            logger.LogError(exception, "Audit sink failed for action {Action} and target {Target}", action, target);
            return false;
        }
    }
}

public static class AuditStorage
{
    private const UnixFileMode DirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const UnixFileMode GroupOrOtherPermissions =
        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    public static void Prepare(string path)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Audit destination must have a parent directory.");
        if (!Directory.Exists(directory))
        {
            if (OperatingSystem.IsLinux()) Directory.CreateDirectory(directory, DirectoryMode);
            else Directory.CreateDirectory(directory);
        }

        if (!OperatingSystem.IsLinux()) return;

        var directoryMode = File.GetUnixFileMode(directory);
        if ((directoryMode & GroupOrOtherPermissions) != 0)
            throw new SecurityException($"Audit directory '{directory}' must not grant group or other permissions.");
        if ((directoryMode & DirectoryMode) != DirectoryMode)
            throw new SecurityException($"Audit directory '{directory}' must grant its owner read, write, and execute permissions.");

        if (!File.Exists(path)) return;
        var fileMode = File.GetUnixFileMode(path);
        if ((fileMode & GroupOrOtherPermissions) != 0 || (fileMode & FileMode) != FileMode)
            throw new SecurityException($"Audit file '{path}' must be owner-readable and owner-writable only.");
    }
}

public sealed class AuditStorageStartupService(
    IOptions<AuditOptions> options,
    IHostEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var destination = options.Value.Destination;
        var path = Path.IsPathFullyQualified(destination)
            ? destination
            : Path.Combine(environment.ContentRootPath, destination);
        AuditStorage.Prepare(path);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
