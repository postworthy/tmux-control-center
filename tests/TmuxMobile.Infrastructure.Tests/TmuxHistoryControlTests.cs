using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class TmuxHistoryControlTests
{
    [Fact]
    public async Task OlderHistoryUsesResolvedTargetAndFixedCopyModeArguments()
    {
        var runner = new RecordingRunner(modeOutput: "0\n");
        var service = CreateService(runner);

        var entered = await service.ControlSessionHistoryAsync(
            SafeIdentifier.ForSession("$test"), TerminalHistoryAction.Older, 17, CancellationToken.None);

        Assert.True(entered);
        Assert.Collection(runner.Requests,
            request => Assert.Equal(["list-sessions", "-F", "#{session_id}"], request.Arguments),
            request => Assert.Equal(
                ["display-message", "-p", "-t", "$test:", "#{pane_in_mode}"], request.Arguments),
            request => Assert.Equal(["copy-mode", "-t", "$test:"], request.Arguments),
            request => Assert.Equal(
                ["send-keys", "-X", "-N", "3", "-t", "$test:", "page-up"], request.Arguments));
        Assert.All(runner.Requests, request => Assert.Equal("/usr/bin/tmux", request.Executable));
    }

    [Fact]
    public async Task NewerHistoryDoesNothingUnlessCopyModeIsActive()
    {
        var runner = new RecordingRunner(modeOutput: "0\n");
        var service = CreateService(runner);

        var entered = await service.ControlSessionHistoryAsync(
            SafeIdentifier.ForSession("$test"), TerminalHistoryAction.Newer, 20, CancellationToken.None);

        Assert.False(entered);
        Assert.Equal(2, runner.Requests.Count);
        Assert.DoesNotContain(runner.Requests, request => request.Arguments.Contains("send-keys"));
    }

    [Fact]
    public async Task LatestCancelsCopyModeWithNoCallerControlledCommand()
    {
        var runner = new RecordingRunner(modeOutput: "1\n");
        var service = CreateService(runner);

        var entered = await service.ControlSessionHistoryAsync(
            SafeIdentifier.ForSession("$test"), TerminalHistoryAction.Latest, 999, CancellationToken.None);

        Assert.False(entered);
        Assert.Equal(["copy-mode", "-q", "-t", "$test:"], runner.Requests[^1].Arguments);
    }

    [LinuxIntegrationFact]
    [Trait("Category", "LinuxIntegration")]
    public async Task RealIsolatedTmuxEntersAndExitsCopyMode()
    {
        var socket = $"tmux-mobile-history-{Guid.NewGuid():N}";
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        try
        {
            Assert.Equal(0, (await RunTmux(runner, socket,
                ["new-session", "-d", "-s", "history-test"], CancellationToken.None)).ExitCode);
            Assert.Equal(0, (await RunTmux(runner, socket,
                ["send-keys", "-t", "history-test:", "seq 1 200", "Enter"], CancellationToken.None)).ExitCode);
            var rawId = (await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "history-test:", "#{session_id}"],
                CancellationToken.None)).StandardOutput.Trim();
            var historySize = 0;
            for (var attempt = 0; attempt < 100 && historySize == 0; attempt++)
            {
                var history = await RunTmux(runner, socket,
                    ["display-message", "-p", "-t", "history-test:", "#{history_size}"],
                    CancellationToken.None);
                historySize = int.Parse(history.StandardOutput.Trim());
                if (historySize == 0) await Task.Delay(10);
            }
            Assert.True(historySize > 0, "The isolated pane must contain history before testing scrollback.");
            var service = CreateService(runner, socket);

            Assert.True(await service.ControlSessionHistoryAsync(SafeIdentifier.ForSession(rawId),
                TerminalHistoryAction.Older, 20, CancellationToken.None));
            var inMode = await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "history-test:", "#{pane_in_mode}"],
                CancellationToken.None);
            Assert.Equal("1", inMode.StandardOutput.Trim());
            var scrollPosition = await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "history-test:", "#{scroll_position}"],
                CancellationToken.None);
            Assert.True(int.Parse(scrollPosition.StandardOutput.Trim()) > 0,
                "Older history must visibly move the tmux copy-mode viewport.");

            await service.ControlSessionHistoryAsync(SafeIdentifier.ForSession(rawId),
                TerminalHistoryAction.Latest, 0, CancellationToken.None);
            var live = await RunTmux(runner, socket,
                ["display-message", "-p", "-t", "history-test:", "#{pane_in_mode}"],
                CancellationToken.None);
            Assert.Equal("0", live.StandardOutput.Trim());
        }
        finally
        {
            await RunTmux(runner, socket, ["kill-server"], CancellationToken.None);
        }
    }

    private static TmuxService CreateService(IProcessRunner runner, string? socket = null) => new(
        runner,
        Options.Create(new TmuxOptions { SocketName = socket }),
        new RuleBasedSessionAnalyzer(new StatusOptions()),
        TimeProvider.System,
        NullLogger<TmuxService>.Instance);

    private static Task<ProcessResult> RunTmux(ProcessRunner runner, string socket,
        IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var all = new List<string> { "-L", socket };
        all.AddRange(arguments);
        return runner.RunAsync(new("/usr/bin/tmux", all, TimeSpan.FromSeconds(5), 8192,
            "test.tmux-history-isolated"), cancellationToken);
    }

    private sealed class RecordingRunner(string modeOutput) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var output = request.Operation switch
            {
                "tmux.resolve-session" => "$test\n",
                "tmux.history-mode" => modeOutput,
                _ => ""
            };
            return Task.FromResult(new ProcessResult(0, output, "", TimeSpan.Zero, false, false));
        }
    }
}
