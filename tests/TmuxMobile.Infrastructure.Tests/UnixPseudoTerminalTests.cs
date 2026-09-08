using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class UnixPseudoTerminalTests
{
    [UnixIntegrationFact]
    [Trait("Category", "UnixIntegration")]
    public async Task ResizeAcceptsHighResolutionContractMaximumAndRejectsAboveIt()
    {
        var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.StartAsync("/bin/bash",
            ["-c", "sleep 300"], new TerminalSize(TerminalSizeLimits.MaximumColumns + 1, 24),
            new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, CancellationToken.None));
        await using var pty = await factory.StartAsync("/bin/bash",
            ["-c", "sleep 300"], new TerminalSize(80, 24),
            new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, CancellationToken.None);

        await pty.ResizeAsync(new(TerminalSizeLimits.MaximumColumns, TerminalSizeLimits.MaximumRows),
            CancellationToken.None);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await pty.ResizeAsync(new(TerminalSizeLimits.MaximumColumns + 1,
                TerminalSizeLimits.MaximumRows), CancellationToken.None));
    }

    [UnixPtyFact]
    public async Task StartAndResizeAreObservableByTheChildProcess()
    {
        // A non-interactive shell that reports its terminal size whenever prompted, so the
        // test never races the child's startup.
        const string script = "printf 'READY\\n'; while read tag; do stty size; printf 'DONE_%s\\n' \"$tag\"; done";
        var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
        await using var pty = await factory.StartAsync("/bin/sh", ["-c", script],
            new TerminalSize(100, 30),
            new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, CancellationToken.None);
        await ReadUntilAsync(pty, "READY");

        // stty reports "rows cols", so the child must see the size forkpty was given.
        AssertSize(await ReportSizeAsync(pty, "ONE"), 30, 100);

        await pty.ResizeAsync(new TerminalSize(120, 40), CancellationToken.None);

        // ioctl(TIOCSWINSZ) must actually reach the kernel, not merely report success.
        AssertSize(await ReportSizeAsync(pty, "TWO"), 40, 120);
        await pty.ResizeAsync(new(TerminalSizeLimits.MaximumColumns, TerminalSizeLimits.MaximumRows), CancellationToken.None);
        AssertSize(await ReportSizeAsync(pty, "MAX"), TerminalSizeLimits.MaximumRows, TerminalSizeLimits.MaximumColumns);
        await pty.ResizeAsync(new(80, 24), CancellationToken.None);
        AssertSize(await ReportSizeAsync(pty, "BACK"), 24, 80);
    }

    [UnixPtyFact]
    public async Task UnicodeOutputAndNaturalExitAreObservable()
    {
        var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
        await using var pty = await factory.StartAsync("/bin/sh", ["-c", "printf 'lambda-λ-done\\n'"],
            new TerminalSize(80, 24), new Dictionary<string, string>(), CancellationToken.None);
        Assert.Contains("lambda-λ-done", await ReadUntilAsync(pty, "done"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await pty.WaitForExitAsync(timeout.Token);
        Assert.True(pty.HasExited);
    }

    private static void AssertSize(string output, int rows, int columns)
    {
        Assert.Matches($@"(?m)^\s*{rows}\s+{columns}\s*$", output);
    }

    /// <summary>Asks the child for its terminal size and returns everything it replied.</summary>
    private static async Task<string> ReportSizeAsync(IPseudoTerminal pty, string tag)
    {
        await pty.Input.WriteAsync(Encoding.UTF8.GetBytes($"{tag}\n"), CancellationToken.None);
        await pty.Input.FlushAsync(CancellationToken.None);
        return await ReadUntilAsync(pty, $"DONE_{tag}");
    }

    private static async Task<string> ReadUntilAsync(IPseudoTerminal pty, string marker)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[4096];
        var output = new StringBuilder();
        while (!output.ToString().Contains(marker, StringComparison.Ordinal))
        {
            var read = await pty.Output.ReadAsync(buffer, timeout.Token).AsTask().WaitAsync(timeout.Token);
            if (read == 0) throw new EndOfStreamException($"PTY closed before {marker}.");
            output.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }
        return output.ToString();
    }

    [UnixIntegrationFact]
    [Trait("Category", "UnixIntegration")]
    public async Task DisconnectingPtyLeavesDedicatedTmuxSessionRunning()
    {
        var socket = $"tmux-mobile-test-{Guid.NewGuid():N}";
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var token = CancellationToken.None;
        try
        {
            var created = await RunTmux(runner, socket, ["new-session", "-d", "-s", "pty-test"], token);
            Assert.Equal(0, created.ExitCode);

            var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
            await using (var pty = await factory.StartAsync(UnixTestEnvironment.TmuxExecutable,
                ["-L", socket, "attach-session", "-t", "pty-test"], new TerminalSize(80, 24),
                new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, token))
            {
                await pty.Input.WriteAsync(Encoding.UTF8.GetBytes("printf PTY_MARKER\\n\r"), token);
                await pty.Input.FlushAsync(token);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var buffer = new byte[8192];
                var output = new StringBuilder();
                while (!output.ToString().Contains("PTY_MARKER", StringComparison.Ordinal))
                {
                    var read = await pty.Output.ReadAsync(buffer, timeout.Token);
                    if (read == 0) break;
                    output.Append(Encoding.UTF8.GetString(buffer, 0, read));
                }
                Assert.Contains("PTY_MARKER", output.ToString());
            }

            var stillRunning = await RunTmux(runner, socket, ["has-session", "-t", "pty-test"], token);
            Assert.Equal(0, stillRunning.ExitCode);
        }
        finally
        {
            await RunTmux(runner, socket, ["kill-server"], CancellationToken.None);
        }
    }

    [UnixIntegrationFact]
    [Trait("Category", "UnixIntegration")]
    public async Task MouseAwareAlternateScreenReceivesWheelEventThroughAttachedTmuxClient()
    {
        var socket = $"tmux-mobile-mouse-{Guid.NewGuid():N}";
        var outputPath = Path.Combine(Path.GetTempPath(), $"tmux-mobile-mouse-{Guid.NewGuid():N}.bin");
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var token = CancellationToken.None;
        try
        {
            var command = $"stty raw -echo; printf '\\033[?1049h\\033[?1003h\\033[?1006hMOUSE_READY\\r\\n'; cat > '{outputPath}'";
            var created = await RunTmux(runner, socket,
                ["new-session", "-d", "-s", "mouse-test", command], token);
            Assert.Equal(0, created.ExitCode);
            var mouseEnabled = await RunTmux(runner, socket, ["set-option", "-g", "mouse", "on"], token);
            Assert.Equal(0, mouseEnabled.ExitCode);

            var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
            await using var pty = await factory.StartAsync(UnixTestEnvironment.TmuxExecutable,
                ["-L", socket, "attach-session", "-t", "mouse-test"], new TerminalSize(80, 24),
                new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, token);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var buffer = new byte[8192];
            var terminalOutput = new StringBuilder();
            while (!terminalOutput.ToString().Contains("MOUSE_READY", StringComparison.Ordinal))
            {
                var read = await pty.Output.ReadAsync(buffer, timeout.Token);
                if (read == 0) break;
                terminalOutput.Append(Encoding.UTF8.GetString(buffer, 0, read));
            }
            Assert.Contains("MOUSE_READY", terminalOutput.ToString());

            var mouseMode = await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "mouse-test:", "#{alternate_on}:#{mouse_any_flag}:#{mouse_sgr_flag}"],
                token);
            Assert.Equal("1:1:1", mouseMode.StandardOutput.Trim());

            await pty.Input.WriteAsync(Encoding.ASCII.GetBytes("\u001b[<64;10;10M"), token);
            await pty.Input.FlushAsync(token);
            for (var attempt = 0; attempt < 100 && (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0); attempt++)
                await Task.Delay(10, token);

            Assert.True(File.Exists(outputPath), "The isolated mouse-aware pane must record forwarded input.");
            var forwarded = Encoding.ASCII.GetString(await File.ReadAllBytesAsync(outputPath, token));
            Assert.Contains("\u001b[<64;", forwarded);

            var paneMode = await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "mouse-test:", "#{pane_in_mode}:#{history_size}"], token);
            Assert.StartsWith("0:", paneMode.StandardOutput.Trim());
        }
        finally
        {
            await RunTmux(runner, socket, ["kill-server"], CancellationToken.None);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [UnixIntegrationFact]
    [Trait("Category", "UnixIntegration")]
    public async Task DisposalKillsStubbornPtyProcessGroup()
    {
        var factory = new UnixPseudoTerminalFactory(NullLoggerFactory.Instance);
        var pty = await factory.StartAsync("/bin/bash",
            ["-c", "trap '' HUP TERM; sleep 300 & printf 'STUBBORN_READY\\n'; wait"], new TerminalSize(80, 24),
            new Dictionary<string, string> { ["TERM"] = "xterm-256color" }, CancellationToken.None);
        var processGroup = pty.ProcessId;
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            var buffer = new byte[256];
            var output = new StringBuilder();
            while (!output.ToString().Contains("STUBBORN_READY", StringComparison.Ordinal))
            {
                var read = await pty.Output.ReadAsync(buffer, timeout.Token);
                if (read == 0) break;
                output.Append(Encoding.UTF8.GetString(buffer, 0, read));
            }
            Assert.Contains("STUBBORN_READY", output.ToString());
        }

        await pty.DisposeAsync();

        Assert.True(pty.HasExited);
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "/usr/bin/pgrep",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { "-g", processGroup.ToString() }
        })!;
        await process.WaitForExitAsync();
        Assert.Equal(1, process.ExitCode);
    }

    private static Task<ProcessResult> RunTmux(
        ProcessRunner runner, string socket, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var all = new List<string> { "-f", "/dev/null", "-L", socket };
        all.AddRange(arguments);
        return runner.RunAsync(new(UnixTestEnvironment.TmuxExecutable, all, TimeSpan.FromSeconds(5), 8192,
            "test.tmux-isolated"), cancellationToken);
    }
}

/// <summary>
/// Runs wherever the PTY layer is supported. Unlike the tmux integration tests this needs
/// nothing but /bin/sh, so it stays enabled by default to guard the native interop contract.
/// </summary>
public sealed class UnixPtyFactAttribute : FactAttribute
{
    public UnixPtyFactAttribute()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            Skip = "PTY support requires Linux or macOS.";
        else if (!File.Exists("/bin/sh"))
            Skip = "PTY test requires /bin/sh.";
    }
}
