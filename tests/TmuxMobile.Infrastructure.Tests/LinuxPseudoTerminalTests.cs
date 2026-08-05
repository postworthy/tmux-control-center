using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class LinuxPseudoTerminalTests
{
    [LinuxIntegrationFact]
    [Trait("Category", "LinuxIntegration")]
    public async Task DisconnectingPtyLeavesDedicatedTmuxSessionRunning()
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/usr/bin/tmux")) return;
        var socket = $"tmux-mobile-test-{Guid.NewGuid():N}";
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var token = CancellationToken.None;
        try
        {
            var created = await RunTmux(runner, socket, ["new-session", "-d", "-s", "pty-test"], token);
            Assert.Equal(0, created.ExitCode);

            var factory = new LinuxPseudoTerminalFactory(NullLoggerFactory.Instance);
            await using (var pty = await factory.StartAsync("/usr/bin/tmux",
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

    [LinuxIntegrationFact]
    [Trait("Category", "LinuxIntegration")]
    public async Task MouseAwareAlternateScreenReceivesWheelEventThroughAttachedTmuxClient()
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/usr/bin/tmux")) return;
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

            var factory = new LinuxPseudoTerminalFactory(NullLoggerFactory.Instance);
            await using var pty = await factory.StartAsync("/usr/bin/tmux",
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

    [LinuxIntegrationFact]
    [Trait("Category", "LinuxIntegration")]
    public async Task DisposalKillsStubbornPtyProcessGroup()
    {
        if (!File.Exists("/bin/bash") || !File.Exists("/usr/bin/pgrep")) return;
        var factory = new LinuxPseudoTerminalFactory(NullLoggerFactory.Instance);
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
        var all = new List<string> { "-L", socket };
        all.AddRange(arguments);
        return runner.RunAsync(new("/usr/bin/tmux", all, TimeSpan.FromSeconds(5), 8192,
            "test.tmux-isolated"), cancellationToken);
    }
}

public sealed class LinuxIntegrationFactAttribute : FactAttribute
{
    public LinuxIntegrationFactAttribute()
    {
        if (!OperatingSystem.IsLinux())
            Skip = "Linux PTY integration requires Linux.";
        else if (Environment.GetEnvironmentVariable("TMUX_MOBILE_RUN_LINUX_INTEGRATION") != "1")
            Skip = "Set TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 to run the isolated real-tmux PTY test.";
    }
}
