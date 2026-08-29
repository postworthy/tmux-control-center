using System.Globalization;
using System.Security;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed record WorkspaceRecoveryStatus(
    bool Enabled,
    bool SnapshotAvailable,
    DateTimeOffset? SnapshotSavedAt,
    bool RequestPending,
    string State,
    DateTimeOffset? UpdatedAt,
    int RestoredSessionCount,
    string? RequestId);

public sealed class WorkspaceRecoveryControl(
    IOptions<WorkspaceRecoveryOptions> options,
    IHostEnvironment environment,
    TimeProvider timeProvider)
{
    private const UnixFileMode DirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const UnixFileMode GroupOrOtherPermissions =
        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
    private static readonly HashSet<string> AllowedStates =
    [
        "idle", "requested", "restored", "blocked-live-sessions", "no-snapshot",
        "invalid-snapshot", "invalid-request", "failed"
    ];
    private readonly SemaphoreSlim gate = new(1, 1);

    public string DirectoryPath => Path.IsPathFullyQualified(options.Value.ControlDirectory)
        ? options.Value.ControlDirectory
        : Path.Combine(environment.ContentRootPath, options.Value.ControlDirectory);

    private string SnapshotPath => Path.Combine(DirectoryPath, "workspace.v1.tsv");
    private string RequestPath => Path.Combine(DirectoryPath, "restore.request");
    private string StatusPath => Path.Combine(DirectoryPath, "status.v1.tsv");

    public void Prepare()
    {
        if (!options.Value.Enabled) return;
        if (IsSymbolicLink(DirectoryPath))
            throw new SecurityException("Workspace recovery directory must not be a symbolic link.");
        if (!Directory.Exists(DirectoryPath))
        {
            if (OperatingSystem.IsLinux()) Directory.CreateDirectory(DirectoryPath, DirectoryMode);
            else Directory.CreateDirectory(DirectoryPath);
        }
        if (!OperatingSystem.IsLinux()) return;
        var mode = File.GetUnixFileMode(DirectoryPath);
        if ((mode & GroupOrOtherPermissions) != 0 || (mode & DirectoryMode) != DirectoryMode)
            throw new SecurityException(
                "Workspace recovery directory must be owner-readable, owner-writable, owner-executable, and private.");
        ValidatePrivateRegularFile(SnapshotPath, required: false);
        ValidatePrivateRegularFile(RequestPath, required: false);
        ValidatePrivateRegularFile(StatusPath, required: false);
    }

    public WorkspaceRecoveryStatus GetStatus()
    {
        if (!options.Value.Enabled)
            return new(false, false, null, false, "disabled", null, 0, null);
        Prepare();
        var snapshotAvailable = File.Exists(SnapshotPath) && !IsSymbolicLink(SnapshotPath);
        DateTimeOffset? snapshotSavedAt = snapshotAvailable
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(SnapshotPath), TimeSpan.Zero)
            : null;
        var requestPending = File.Exists(RequestPath) && !IsSymbolicLink(RequestPath);
        var state = requestPending ? "requested" : snapshotAvailable ? "idle" : "no-snapshot";
        DateTimeOffset? updatedAt = null;
        var restoredSessionCount = 0;
        string? requestId = null;
        if (File.Exists(StatusPath) && !IsSymbolicLink(StatusPath))
        {
            ValidatePrivateRegularFile(StatusPath, required: true);
            var fields = File.ReadAllText(StatusPath, Encoding.UTF8).TrimEnd('\r', '\n').Split('\t');
            if (fields.Length == 5 && fields[0] == "1" && AllowedStates.Contains(fields[1]) &&
                long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var unix) &&
                int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count >= 0 &&
                Guid.TryParseExact(fields[4], "D", out var parsedRequestId))
            {
                state = requestPending ? "requested" : fields[1];
                updatedAt = DateTimeOffset.FromUnixTimeSeconds(unix);
                restoredSessionCount = count;
                requestId = parsedRequestId.ToString("D");
            }
            else state = "failed";
        }
        return new(true, snapshotAvailable, snapshotSavedAt, requestPending, state,
            updatedAt, restoredSessionCount, requestId);
    }

    public async Task<Guid> RequestRestoreAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) throw new WorkspaceRecoveryDisabledException();
        await gate.WaitAsync(cancellationToken);
        try
        {
            Prepare();
            if (!File.Exists(SnapshotPath) || IsSymbolicLink(SnapshotPath))
                throw new WorkspaceSnapshotUnavailableException();
            ValidatePrivateRegularFile(SnapshotPath, required: true);
            if (File.Exists(RequestPath)) throw new WorkspaceRestorePendingException();

            var requestId = Guid.NewGuid();
            var temporary = Path.Combine(DirectoryPath, $".restore-request-{requestId:N}");
            try
            {
                var content = $"1\t{requestId:D}\t{timeProvider.GetUtcNow().ToUnixTimeSeconds()}\n";
                var streamOptions = new FileStreamOptions
                {
                    Mode = System.IO.FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough
                };
                if (OperatingSystem.IsLinux()) streamOptions.UnixCreateMode = PrivateFileMode;
                await using (var stream = new FileStream(temporary, streamOptions))
                {
                    var bytes = Encoding.UTF8.GetBytes(content);
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
                File.Move(temporary, RequestPath, overwrite: false);
                return requestId;
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
        finally { gate.Release(); }
    }

    private static bool IsSymbolicLink(string path) =>
        File.Exists(path) || Directory.Exists(path)
            ? (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0
            : false;

    private static void ValidatePrivateRegularFile(string path, bool required)
    {
        if (!File.Exists(path))
        {
            if (required) throw new FileNotFoundException("Workspace recovery state is unavailable.", path);
            return;
        }
        if (IsSymbolicLink(path)) throw new SecurityException("Workspace recovery state must not be a symbolic link.");
        if (!OperatingSystem.IsLinux()) return;
        var mode = File.GetUnixFileMode(path);
        if ((mode & GroupOrOtherPermissions) != 0 || (mode & PrivateFileMode) != PrivateFileMode)
            throw new SecurityException("Workspace recovery state must be owner-readable and owner-writable only.");
    }
}

public sealed class WorkspaceRecoveryStartupService(WorkspaceRecoveryControl control) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        control.Prepare();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class WorkspaceRecoveryDisabledException : Exception;
public sealed class WorkspaceSnapshotUnavailableException : Exception;
public sealed class WorkspaceRestorePendingException : Exception;
