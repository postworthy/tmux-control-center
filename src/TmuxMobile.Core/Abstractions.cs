namespace TmuxMobile.Core;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
}

public interface ITmuxService
{
    Task<IReadOnlyList<TmuxSession>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<CreatedTmuxSession> CreateSessionAsync(string name, CancellationToken cancellationToken);
    Task<TmuxSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TmuxPane>> GetPanesAsync(string sessionId, CancellationToken cancellationToken);
    Task<TmuxTopology> GetTopologyAsync(string sessionId, CancellationToken cancellationToken);
    Task<CreatedTmuxWindow> CreateWindowAsync(string sessionId, string? name, CancellationToken cancellationToken);
    Task SelectWindowAsync(string windowId, CancellationToken cancellationToken);
    Task KillWindowAsync(string windowId, CancellationToken cancellationToken);
    Task<CreatedTmuxPane> SplitPaneAsync(string paneId, TmuxSplitOrientation orientation,
        CancellationToken cancellationToken);
    Task SelectPaneAsync(string paneId, CancellationToken cancellationToken);
    Task ResizePaneAsync(string paneId, TmuxResizeDirection direction, int cells,
        CancellationToken cancellationToken);
    Task KillPaneAsync(string paneId, CancellationToken cancellationToken);
    Task<string> CapturePaneAsync(string paneId, int historyLines, CancellationToken cancellationToken);
    Task RenameSessionAsync(string sessionId, string newName, CancellationToken cancellationToken);
    Task KillSessionAsync(string sessionId, CancellationToken cancellationToken);
    Task SendKeysAsync(string paneId, IReadOnlyList<TmuxKey> keys, CancellationToken cancellationToken);
    Task SendTextAsync(string paneId, string text, CancellationToken cancellationToken);
    Task InterruptPaneAsync(string paneId, CancellationToken cancellationToken);
    Task<bool> ControlSessionHistoryAsync(string sessionId, TerminalHistoryAction action, int pages,
        CancellationToken cancellationToken);
}

public interface ISessionAnalyzer
{
    (SessionStatus Status, string Reason) Analyze(SessionAnalysisInput input);
}

public sealed record SessionAnalysisInput(
    bool IsAttached,
    DateTimeOffset LastActivityAt,
    string CurrentCommand,
    string PreviewText,
    DateTimeOffset Now);

public interface IPseudoTerminal : IAsyncDisposable
{
    Stream Input { get; }
    Stream Output { get; }
    int ProcessId { get; }
    bool HasExited { get; }
    ValueTask ResizeAsync(TerminalSize size, CancellationToken cancellationToken);
    Task WaitForExitAsync(CancellationToken cancellationToken);
}

public interface IPseudoTerminalFactory
{
    Task<IPseudoTerminal> StartAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TerminalSize size,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken);
}

public interface ITmuxTargetResolver
{
    Task<string?> ResolveRawSessionAsync(string safeSessionId, CancellationToken cancellationToken);
}

public interface IInventoryStore
{
    InventorySnapshot Current { get; }
    event Action<InventorySnapshot>? Changed;
    Task RefreshAsync(CancellationToken cancellationToken);
}

public interface IAuditLogger
{
    Task<bool> WriteAsync(string action, string subject, string target, bool succeeded,
        CancellationToken cancellationToken);
}
