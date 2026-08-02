using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed class LinuxPseudoTerminalFactory(ILoggerFactory loggerFactory) : IPseudoTerminalFactory
{
    public Task<IPseudoTerminal> StartAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TerminalSize size,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("PTY support currently requires Linux.");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IPseudoTerminal>(
            LinuxPseudoTerminal.Start(executable, arguments, size, environment,
                loggerFactory.CreateLogger<LinuxPseudoTerminal>()));
    }
}

public sealed class LinuxPseudoTerminal : IPseudoTerminal
{
    private const int Tiocswinsz = 0x5414;
    private const int SigHup = 1;
    private const int SigKill = 9;
    private const int SigTerm = 15;
    private const int ErrorInterrupted = 4;
    private const int ErrorNoSuchProcess = 3;
    private readonly FileStream _input;
    private readonly FileStream _output;
    private readonly ILogger _logger;
    private readonly Task _waitTask;
    private int _disposed;
    private int _exitStatus;

    private LinuxPseudoTerminal(int masterFd, int processId, ILogger logger)
    {
        ProcessId = processId;
        _logger = logger;
        var inputFd = Native.dup(masterFd);
        if (inputFd < 0) throw new InvalidOperationException("Unable to duplicate PTY descriptor.");
        _output = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)masterFd, true),
            FileAccess.Read, 4096, isAsync: false);
        _input = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)inputFd, true),
            FileAccess.Write, 4096, isAsync: false);
        _waitTask = Task.Run(() =>
        {
            int result;
            do { result = Native.waitpid(ProcessId, out _exitStatus, 0); }
            while (result < 0 && Marshal.GetLastPInvokeError() == ErrorInterrupted);
            if (result < 0)
                _logger.LogWarning("waitpid failed for PTY child {ProcessId} with errno {Errno}",
                    ProcessId, Marshal.GetLastPInvokeError());
        });
    }

    public Stream Input => _input;
    public Stream Output => _output;
    public int ProcessId { get; }
    public bool HasExited => _waitTask.IsCompleted;

    public static LinuxPseudoTerminal Start(
        string executable,
        IReadOnlyList<string> arguments,
        TerminalSize size,
        IReadOnlyDictionary<string, string> environment,
        ILogger logger)
    {
        var execExecutable = environment.Count == 0 ? executable : "/usr/bin/env";
        var argv = environment.Count == 0
            ? new[] { executable }.Concat(arguments).ToArray()
            : new[] { execExecutable }
                .Concat(environment.Select(pair => $"{pair.Key}={pair.Value}"))
                .Append(executable)
                .Concat(arguments)
                .ToArray();
        var argvPointers = argv.Select(Marshal.StringToHGlobalAnsi).ToArray();
        var executablePointer = Marshal.StringToHGlobalAnsi(execExecutable);
        var argvBlock = Marshal.AllocHGlobal(IntPtr.Size * (argvPointers.Length + 1));
        for (var i = 0; i < argvPointers.Length; i++) Marshal.WriteIntPtr(argvBlock, i * IntPtr.Size, argvPointers[i]);
        Marshal.WriteIntPtr(argvBlock, argvPointers.Length * IntPtr.Size, IntPtr.Zero);
        try
        {
            var winsize = new Native.WinSize((ushort)size.Rows, (ushort)size.Columns, 0, 0);
            var pid = Native.tmux_mobile_forkpty_exec(out var master, ref winsize,
                executablePointer, argvBlock);
            if (pid < 0) throw new InvalidOperationException($"forkpty failed with errno {Marshal.GetLastPInvokeError()}.");
            logger.LogInformation("Started PTY child {ProcessId}", pid);
            return new LinuxPseudoTerminal(master, pid, logger);
        }
        finally
        {
            foreach (var pointer in argvPointers) Marshal.FreeHGlobal(pointer);
            Marshal.FreeHGlobal(executablePointer);
            Marshal.FreeHGlobal(argvBlock);
        }
    }

    public ValueTask ResizeAsync(TerminalSize size, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (size.Columns is < 10 or > 500 || size.Rows is < 5 or > 300)
            throw new ArgumentOutOfRangeException(nameof(size));
        var winsize = new Native.WinSize((ushort)size.Rows, (ushort)size.Columns, 0, 0);
        var descriptor = _output.SafeFileHandle.DangerousGetHandle().ToInt32();
        if (Native.ioctl(descriptor, Tiocswinsz, ref winsize) < 0)
            throw new InvalidOperationException("Unable to resize PTY.");
        return ValueTask.CompletedTask;
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken) => _waitTask.WaitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            if (!_waitTask.IsCompleted)
            {
                if (!await SignalGroupAndWaitAsync(SigHup, "SIGHUP"))
                {
                    _logger.LogInformation("Escalating PTY process group {ProcessId} cleanup to SIGTERM", ProcessId);
                    if (!await SignalGroupAndWaitAsync(SigTerm, "SIGTERM"))
                    {
                        _logger.LogWarning("Forcing PTY process group {ProcessId} cleanup with SIGKILL", ProcessId);
                        if (!await SignalGroupAndWaitAsync(SigKill, "SIGKILL"))
                            throw new TimeoutException($"PTY process group {ProcessId} did not exit after SIGKILL.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanly stop PTY child {ProcessId}", ProcessId);
        }
        finally
        {
            await _input.DisposeAsync();
            await _output.DisposeAsync();
            _logger.LogInformation("Cleaned up PTY child {ProcessId}", ProcessId);
        }
    }

    private async Task<bool> SignalGroupAndWaitAsync(int signal, string signalName)
    {
        var result = Native.kill(-ProcessId, signal);
        if (result < 0 && Marshal.GetLastPInvokeError() != ErrorNoSuchProcess)
            _logger.LogWarning("Unable to send {Signal} to PTY process group {ProcessId}; errno {Errno}",
                signalName, ProcessId, Marshal.GetLastPInvokeError());
        try
        {
            await _waitTask.WaitAsync(TimeSpan.FromSeconds(2));
            return true;
        }
        catch (TimeoutException) { return false; }
    }

    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct WinSize(ushort row, ushort col, ushort xpixel, ushort ypixel)
        {
            public ushort Row = row;
            public ushort Col = col;
            public ushort XPixel = xpixel;
            public ushort YPixel = ypixel;
        }

        [DllImport("tmuxmobilepty", SetLastError = true)]
        internal static extern int tmux_mobile_forkpty_exec(
            out int master, ref WinSize winsize, IntPtr executable, IntPtr argv);
        [DllImport("libc", SetLastError = true)] internal static extern int dup(int fd);
        [DllImport("libc", SetLastError = true)] internal static extern int ioctl(int fd, int request, ref WinSize value);
        [DllImport("libc", SetLastError = true)] internal static extern int kill(int pid, int signal);
        [DllImport("libc", SetLastError = true)] internal static extern int waitpid(int pid, out int status, int options);
    }
}
