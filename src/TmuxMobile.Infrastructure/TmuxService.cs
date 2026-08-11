using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using System.Collections.Concurrent;

namespace TmuxMobile.Infrastructure;

public sealed class TmuxService(
    IProcessRunner runner,
    IOptions<TmuxOptions> options,
    ISessionAnalyzer analyzer,
    TimeProvider timeProvider,
    ILogger<TmuxService> logger) : ITmuxService, ITmuxTargetResolver
{
    private const string SessionFormat =
        "#{session_id}" + TmuxParser.Separator +
        "#{session_name}" + TmuxParser.Separator +
        "#{session_created}" + TmuxParser.Separator +
        "#{session_activity}" + TmuxParser.Separator +
        "#{?session_attached,1,0}" + TmuxParser.Separator +
        "#{session_attached}" + TmuxParser.Separator +
        "#{session_windows}" + TmuxParser.Separator +
        "#{window_panes}" + TmuxParser.Separator +
        "#{window_name}" + TmuxParser.Separator +
        "#{pane_id}" + TmuxParser.Separator +
        "#{pane_current_command}" + TmuxParser.Separator +
        "#{pane_current_path}" + TmuxParser.Separator +
        "#{pane_title}" + TmuxParser.Separator +
        "#{?window_active,1,0}" + TmuxParser.Separator +
        "#{?pane_active,1,0}";
    private const string PaneFormat =
        "#{pane_id}" + TmuxParser.Separator +
        "#{session_id}" + TmuxParser.Separator +
        "#{window_index}" + TmuxParser.Separator +
        "#{pane_index}" + TmuxParser.Separator +
        "#{pane_title}" + TmuxParser.Separator +
        "#{pane_current_command}" + TmuxParser.Separator +
        "#{pane_current_path}" + TmuxParser.Separator +
        "#{pane_active}" + TmuxParser.Separator +
        "#{pane_pid}" + TmuxParser.Separator +
        "#{pane_width}" + TmuxParser.Separator +
        "#{pane_height}";
    private readonly ConcurrentDictionary<string, PreviewCacheEntry> _previews = new();

    public async Task<IReadOnlyList<TmuxSession>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var result = await RunAsync(["list-panes", "-a", "-F", SessionFormat], "tmux.list-sessions", cancellationToken,
            tolerateNoServer: true);
        if (string.IsNullOrWhiteSpace(result)) return [];
        var raw = TmuxParser.ParseSessions(result);
        var sessions = new List<TmuxSession>();
        foreach (var group in raw.GroupBy(x => x.TmuxId))
        {
            var active = group.FirstOrDefault(x => x.WindowActive && x.PaneActive) ??
                         group.FirstOrDefault(x => x.PaneActive) ?? group.First();
            var now = timeProvider.GetUtcNow();
            string preview;
            bool truncated;
            if (_previews.TryGetValue(active.PaneTmuxId, out var cached) &&
                now - cached.CapturedAt < TimeSpan.FromSeconds(options.Value.PreviewRefreshIntervalSeconds))
            {
                preview = cached.Text;
                truncated = cached.Truncated;
            }
            else
            {
                preview = "";
                truncated = false;
                try
                {
                    preview = await CaptureRawPaneAsync(active.PaneTmuxId,
                        Math.Min(options.Value.CardPreviewLines, options.Value.MaxCaptureLines), cancellationToken);
                    preview = TerminalOutput.Sanitize(preview, options.Value.MaxCaptureBytes, out truncated);
                    _previews[active.PaneTmuxId] = new(now, preview, truncated);
                }
                catch (Exception ex) when (ex is TmuxCommandException or TmuxNotFoundException)
                {
                    logger.LogWarning("Unable to refresh preview for session {SessionId}", SafeIdentifier.ForSession(group.Key));
                }
            }
            var clients = active.Clients;
            var lastActivity = DateTimeOffset.FromUnixTimeSeconds(active.ActivityUnix);
            var inferred = analyzer.Analyze(new(active.Attached, lastActivity, active.Command, preview,
                timeProvider.GetUtcNow()));
            sessions.Add(new(
                SafeIdentifier.ForSession(group.Key), active.Name,
                DateTimeOffset.FromUnixTimeSeconds(active.CreatedUnix), lastActivity,
                active.Attached, clients,
                active.Windows, group.Select(x => x.PaneTmuxId).Distinct().Count(),
                active.WindowName, SafeIdentifier.ForPane(active.PaneTmuxId), active.Command,
                active.WorkingDirectory, active.Title, inferred.Status, inferred.Reason, preview, truncated));
        }
        return sessions.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<CreatedTmuxSession> CreateSessionAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = InputValidation.ValidateCreateName(name);
        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.Value.SocketName))
        {
            arguments.Add("-L");
            arguments.Add(options.Value.SocketName);
        }
        arguments.AddRange(["new-session", "-d", "-P", "-F", "#{session_id}", "-s", normalized]);
        var result = await runner.RunAsync(new(options.Value.ExecutablePath, arguments,
            TimeSpan.FromSeconds(options.Value.ProcessTimeoutSeconds), options.Value.MaxCaptureBytes,
            "tmux.create-session"), cancellationToken);
        if (result.TimedOut) throw new TmuxCommandException("tmux command timed out.");
        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("duplicate session", StringComparison.OrdinalIgnoreCase))
                throw new TmuxConflictException("A session with that name already exists.");
            throw new TmuxCommandException($"tmux operation failed with exit code {result.ExitCode}.");
        }

        var rawId = result.StandardOutput.Trim();
        if (rawId.Length < 2 || rawId[0] != '$' || !int.TryParse(rawId.AsSpan(1), out _))
            throw new TmuxCommandException("tmux returned an invalid session identifier.");
        return new(SafeIdentifier.ForSession(rawId), normalized);
    }

    public async Task<TmuxSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!SafeIdentifier.IsSession(sessionId)) return null;
        return (await GetSessionsAsync(cancellationToken)).SingleOrDefault(x => x.Id == sessionId);
    }

    public async Task<IReadOnlyList<TmuxPane>> GetPanesAsync(string sessionId, CancellationToken cancellationToken)
    {
        var rawSession = await ResolveSessionAsync(sessionId, cancellationToken);
        if (rawSession is null) throw new TmuxNotFoundException("Session was not found.");
        var output = await RunAsync(["list-panes", "-t", rawSession, "-F", PaneFormat], "tmux.list-panes",
            cancellationToken);
        return TmuxParser.ParsePanes(output).Select(x => new TmuxPane(
            SafeIdentifier.ForPane(x.TmuxId), SafeIdentifier.ForSession(x.SessionTmuxId),
            x.WindowIndex, x.PaneIndex, x.Title, x.Command, x.WorkingDirectory, x.Active,
            x.ProcessId, x.Width, x.Height)).ToArray();
    }

    public async Task<string> CapturePaneAsync(string paneId, int historyLines, CancellationToken cancellationToken)
    {
        var rawPane = await ResolvePaneAsync(paneId, cancellationToken);
        if (rawPane is null) throw new TmuxNotFoundException("Pane was not found.");
        var lines = Math.Clamp(historyLines, 1, options.Value.MaxCaptureLines);
        var raw = await CaptureRawPaneAsync(rawPane, lines, cancellationToken);
        return TerminalOutput.Sanitize(raw, options.Value.MaxCaptureBytes, out _);
    }

    public async Task RenameSessionAsync(string sessionId, string newName, CancellationToken cancellationToken)
    {
        var raw = await ResolveSessionAsync(sessionId, cancellationToken)
            ?? throw new TmuxNotFoundException("Session was not found.");
        await RunAsync(["rename-session", "-t", raw, InputValidation.ValidateRename(newName)],
            "tmux.rename-session", cancellationToken);
    }

    public async Task SendKeysAsync(string paneId, IReadOnlyList<TmuxKey> keys, CancellationToken cancellationToken)
    {
        if (keys.Count is < 1 or > 32) throw new ArgumentException("Between 1 and 32 keys are required.");
        var raw = await ResolvePaneAsync(paneId, cancellationToken)
            ?? throw new TmuxNotFoundException("Pane was not found.");
        var args = new List<string> { "send-keys", "-t", raw };
        args.AddRange(keys.Select(key => TerminalKeyEncoder.ToTmuxKey(key, options.Value.Prefix)));
        await RunAsync(args, "tmux.send-keys", cancellationToken);
    }

    public async Task SendTextAsync(string paneId, string text, CancellationToken cancellationToken)
    {
        var raw = await ResolvePaneAsync(paneId, cancellationToken)
            ?? throw new TmuxNotFoundException("Pane was not found.");
        await RunAsync(["send-keys", "-l", "-t", raw, "--", InputValidation.ValidateText(text)],
            "tmux.send-text", cancellationToken);
    }

    public Task InterruptPaneAsync(string paneId, CancellationToken cancellationToken) =>
        SendKeysAsync(paneId, [TmuxKey.ControlC], cancellationToken);

    public async Task<bool> ControlSessionHistoryAsync(string sessionId, TerminalHistoryAction action, int pages,
        CancellationToken cancellationToken)
    {
        var raw = await ResolveSessionAsync(sessionId, cancellationToken)
            ?? throw new TmuxNotFoundException("Session was not found.");
        var target = $"{raw}:";

        if (action == TerminalHistoryAction.Latest)
        {
            await RunAsync(["copy-mode", "-q", "-t", target], "tmux.history-latest", cancellationToken);
            return false;
        }

        var boundedPages = Math.Clamp(pages, 1, 3);
        var inMode = (await RunAsync(["display-message", "-p", "-t", target, "#{pane_in_mode}"],
            "tmux.history-mode", cancellationToken)).Trim() == "1";
        if (action == TerminalHistoryAction.Newer && !inMode) return false;

        var enteredCopyMode = false;
        if (!inMode)
        {
            await RunAsync(["copy-mode", "-t", target], "tmux.history-enter", cancellationToken);
            enteredCopyMode = true;
        }
        var command = action == TerminalHistoryAction.Older ? "page-up" : "page-down";
        await RunAsync(["send-keys", "-X", "-N", boundedPages.ToString(), "-t", target, command],
            "tmux.history-scroll", cancellationToken);
        return enteredCopyMode;
    }

    private Task<string> CaptureRawPaneAsync(string rawPane, int lines, CancellationToken cancellationToken) =>
        RunAsync(["capture-pane", "-p", "-J", "-t", rawPane, "-S", $"-{lines}"],
            "tmux.capture-pane", cancellationToken);

    private async Task<string?> ResolveSessionAsync(string safeId, CancellationToken cancellationToken)
    {
        if (!SafeIdentifier.IsSession(safeId)) return null;
        var output = await RunAsync(["list-sessions", "-F", "#{session_id}"], "tmux.resolve-session",
            cancellationToken, tolerateNoServer: true);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(raw => SafeIdentifier.ForSession(raw.Trim()) == safeId)?.Trim();
    }

    private async Task<string?> ResolvePaneAsync(string safeId, CancellationToken cancellationToken)
    {
        if (!SafeIdentifier.IsPane(safeId)) return null;
        var output = await RunAsync(["list-panes", "-a", "-F", "#{pane_id}"], "tmux.resolve-pane",
            cancellationToken, tolerateNoServer: true);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(raw => SafeIdentifier.ForPane(raw.Trim()) == safeId)?.Trim();
    }

    public async Task<string?> ResolveRawSessionAsync(string safeId, CancellationToken cancellationToken) =>
        await ResolveSessionAsync(safeId, cancellationToken);

    private async Task<string> RunAsync(
        IReadOnlyList<string> arguments,
        string operation,
        CancellationToken cancellationToken,
        bool tolerateNoServer = false)
    {
        var actual = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.Value.SocketName))
        {
            actual.Add("-L");
            actual.Add(options.Value.SocketName);
        }
        actual.AddRange(arguments);
        var result = await runner.RunAsync(new(options.Value.ExecutablePath, actual,
            TimeSpan.FromSeconds(options.Value.ProcessTimeoutSeconds), options.Value.MaxCaptureBytes, operation),
            cancellationToken);
        if (result.TimedOut) throw new TmuxCommandException("tmux command timed out.");
        if (result.ExitCode != 0)
        {
            if (tolerateNoServer && (result.StandardError.Contains("no server running", StringComparison.OrdinalIgnoreCase) ||
                                     result.StandardError.Contains("failed to connect", StringComparison.OrdinalIgnoreCase)))
                return "";
            throw new TmuxCommandException($"tmux operation failed with exit code {result.ExitCode}.");
        }
        return result.StandardOutput;
    }

    private sealed record PreviewCacheEntry(DateTimeOffset CapturedAt, string Text, bool Truncated);
}
