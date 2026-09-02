using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class TmuxTopologyTests
{
    [LinuxIntegrationFact]
    [Trait("Category", "LinuxIntegration")]
    public async Task RealIsolatedTmuxTopologyRoundTripsWithoutKillingSession()
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/usr/bin/tmux")) return;
        var socket = $"tmux-mobile-topology-{Guid.NewGuid():N}";
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);
        var service = CreateService(runner, socket);
        try
        {
            var session = await service.CreateSessionAsync("topology-test", CancellationToken.None);
            var original = Assert.Single((await service.GetTopologyAsync(session.Id, CancellationToken.None)).Windows);
            var created = await service.CreateWindowAsync(session.Id, "editor", CancellationToken.None);
            await service.SelectWindowAsync(created.Id, CancellationToken.None);
            var selected = (await service.GetTopologyAsync(session.Id, CancellationToken.None)).Windows
                .Single(window => window.Id == created.Id);
            Assert.True(selected.IsActive);

            var firstPane = Assert.Single(selected.Panes);
            var split = await service.SplitPaneAsync(firstPane.Id, TmuxSplitOrientation.Horizontal,
                CancellationToken.None);
            var beforeResize = (await service.GetTopologyAsync(session.Id, CancellationToken.None)).Windows
                .Single(window => window.Id == created.Id);
            Assert.Equal(2, beforeResize.Panes.Count);
            await service.SelectPaneAsync(firstPane.Id, CancellationToken.None);
            await service.ResizePaneAsync(firstPane.Id, TmuxResizeDirection.Right, 2, CancellationToken.None);
            var afterResize = (await service.GetTopologyAsync(session.Id, CancellationToken.None)).Windows
                .Single(window => window.Id == created.Id);
            Assert.NotEqual(beforeResize.Panes.Single(pane => pane.Id == firstPane.Id).Width,
                afterResize.Panes.Single(pane => pane.Id == firstPane.Id).Width);

            await service.KillPaneAsync(split.Id, CancellationToken.None);
            await service.KillWindowAsync(original.Id, CancellationToken.None);
            var final = Assert.Single((await service.GetTopologyAsync(session.Id, CancellationToken.None)).Windows);
            await Assert.ThrowsAsync<TmuxConflictException>(() =>
                service.KillWindowAsync(final.Id, CancellationToken.None));
            await Assert.ThrowsAsync<TmuxConflictException>(() =>
                service.KillPaneAsync(Assert.Single(final.Panes).Id, CancellationToken.None));
            Assert.NotNull(await service.GetSessionAsync(session.Id, CancellationToken.None));
        }
        finally
        {
            var request = new ProcessRequest("/usr/bin/tmux", ["-L", socket, "kill-server"],
                TimeSpan.FromSeconds(5), 8192, "test.tmux-topology-cleanup");
            await runner.RunAsync(request, CancellationToken.None);
        }
    }

    [Fact]
    public async Task TopologyMapsOpaqueWindowAndPaneIdentifiers()
    {
        var separator = TmuxParser.Separator;
        var pane = string.Join(separator, "%2", "$1", "@3", "1", "0", "editor", "1", "layout",
            "title", "bash", "/work", "1", "42", "100", "30");
        var runner = new RecordingRunner(Ok("$1\n"), Ok(pane));
        var service = CreateService(runner);

        var topology = await service.GetTopologyAsync(SafeIdentifier.ForSession("$1"), CancellationToken.None);

        var window = Assert.Single(topology.Windows);
        Assert.Equal(SafeIdentifier.ForWindow("@3"), window.Id);
        Assert.Equal("editor", window.Name);
        Assert.Equal(SafeIdentifier.ForPane("%2"), Assert.Single(window.Panes).Id);
        Assert.Equal(["list-sessions", "-F", "#{session_id}"], runner.Requests[0].Arguments);
        Assert.Equal("list-panes", runner.Requests[1].Arguments[0]);
        Assert.Contains("-s", runner.Requests[1].Arguments);
    }

    [Fact]
    public async Task SplitAndResizeUseOnlyClosedFixedArguments()
    {
        var runner = new RecordingRunner(Ok("%1\n"), Ok("%2\n"), Ok("%1\n%2\n"), Ok());
        var service = CreateService(runner, "topology-test");

        var created = await service.SplitPaneAsync(SafeIdentifier.ForPane("%1"),
            TmuxSplitOrientation.Horizontal, CancellationToken.None);
        await service.ResizePaneAsync(SafeIdentifier.ForPane("%2"), TmuxResizeDirection.Left, 4,
            CancellationToken.None);

        Assert.Equal(SafeIdentifier.ForPane("%2"), created.Id);
        Assert.Equal(["-L", "topology-test", "split-window", "-d", "-P", "-F", "#{pane_id}",
            "-h", "-t", "%1"], runner.Requests[1].Arguments);
        Assert.Equal(["-L", "topology-test", "resize-pane", "-L", "-t", "%2", "4"],
            runner.Requests[3].Arguments);
        Assert.All(runner.Requests, request => Assert.DoesNotContain("run-shell", request.Arguments));
    }

    [Fact]
    public async Task FinalWindowCloseIsAtomicallyRefused()
    {
        var runner = new RecordingRunner(Ok("@1\n"), Ok("TMUXCTL_LAST_WINDOW\n"));
        var service = CreateService(runner);

        await Assert.ThrowsAsync<TmuxConflictException>(() =>
            service.KillWindowAsync(SafeIdentifier.ForWindow("@1"), CancellationToken.None));

        Assert.Equal(["if-shell", "-F", "-t", "@1", "#{==:#{session_windows},1}",
            "display-message -p TMUXCTL_LAST_WINDOW", "kill-window -t @1"], runner.Requests[1].Arguments);
    }

    private static ProcessResult Ok(string output = "") => new(0, output, "", TimeSpan.Zero, false, false);

    private static TmuxService CreateService(IProcessRunner runner, string? socket = null) => new(
        runner, Options.Create(new TmuxOptions { SocketName = socket }),
        new RuleBasedSessionAnalyzer(new StatusOptions()), TimeProvider.System,
        NullLogger<TmuxService>.Instance);

    private sealed class RecordingRunner(params ProcessResult[] results) : IProcessRunner
    {
        private readonly Queue<ProcessResult> remaining = new(results);
        public List<ProcessRequest> Requests { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(remaining.Dequeue());
        }
    }
}
