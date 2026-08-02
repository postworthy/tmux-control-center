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
