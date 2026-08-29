namespace TmuxMobile.Core;

public enum SessionStatus
{
    Active, Idle, Attached, Detached, Waiting, Completed, Failed, Unknown
}

public sealed record TmuxSession(
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    bool IsAttached,
    int AttachedClientCount,
    int WindowCount,
    int PaneCount,
    string CurrentWindowName,
    string CurrentPaneId,
    string CurrentCommand,
    string CurrentWorkingDirectory,
    string Title,
    SessionStatus Status,
    string StatusReason,
    string PreviewText,
    bool PreviewTruncated = false);

public sealed record TmuxPane(
    string Id,
    string SessionId,
    int WindowIndex,
    int PaneIndex,
    string Title,
    string CurrentCommand,
    string CurrentWorkingDirectory,
    bool IsActive,
    int ProcessId,
    int Width,
    int Height,
    string WindowId = "",
    string WindowName = "",
    bool IsWindowActive = false,
    string WindowLayout = "");

public sealed record TmuxWindow(
    string Id,
    string SessionId,
    int Index,
    string Name,
    bool IsActive,
    string Layout,
    IReadOnlyList<TmuxPane> Panes);

public sealed record TmuxTopology(string SessionId, IReadOnlyList<TmuxWindow> Windows);

public enum TmuxSplitOrientation { Horizontal, Vertical }
public enum TmuxResizeDirection { Left, Right, Up, Down }

public enum TmuxKey
{
    Enter, Escape, Tab, Up, Down, Left, Right, Backspace, Delete, Home, End,
    PageUp, PageDown, ControlC, ControlD, TmuxPrefix
}

public enum TerminalHistoryAction
{
    Older, Newer, Latest
}

public sealed record ProcessRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    int MaxOutputBytes,
    string Operation);

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool WasTruncated,
    bool TimedOut);

public sealed record TerminalSize(int Columns, int Rows);
public sealed record InventorySnapshot(long Version, DateTimeOffset UpdatedAt, IReadOnlyList<TmuxSession> Sessions);
public sealed record CreatedTmuxSession(string Id, string Name);
public sealed record CreatedTmuxWindow(string Id);
public sealed record CreatedTmuxPane(string Id);

public sealed class TmuxNotFoundException(string message) : Exception(message);
public sealed class TmuxConflictException(string message) : Exception(message);
public sealed class TmuxCommandException(string message) : Exception(message);
