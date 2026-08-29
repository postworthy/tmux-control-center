using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class TmuxSessionTerminationTests
{
    [Fact]
    public async Task ResolvesOpaqueIdAndUsesFixedKillArguments()
    {
        var runner = new RecordingRunner(
            new ProcessResult(0, "$41\n$42\n", "", TimeSpan.Zero, false, false),
            new ProcessResult(0, "", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner, "mobile-test");

        await service.KillSessionAsync(SafeIdentifier.ForSession("$42"), CancellationToken.None);

        Assert.Collection(runner.Requests,
            resolve =>
            {
                Assert.Equal(["-L", "mobile-test", "list-sessions", "-F", "#{session_id}"], resolve.Arguments);
                Assert.Equal("tmux.resolve-session", resolve.Operation);
            },
            kill =>
            {
                Assert.Equal(["-L", "mobile-test", "kill-session", "-t", "$42"], kill.Arguments);
                Assert.Equal("tmux.kill-session", kill.Operation);
                Assert.Equal("/usr/bin/tmux", kill.Executable);
            });
    }

    [Fact]
    public async Task RejectsUnknownOpaqueIdBeforeKillInvocation()
    {
        var runner = new RecordingRunner(
            new ProcessResult(0, "$41\n", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        await Assert.ThrowsAsync<TmuxNotFoundException>(() =>
            service.KillSessionAsync(SafeIdentifier.ForSession("$42"), CancellationToken.None));

        var resolve = Assert.Single(runner.Requests);
        Assert.Equal("tmux.resolve-session", resolve.Operation);
    }

    [Fact]
    public async Task RejectsMalformedOpaqueIdWithoutInvokingTmux()
    {
        var runner = new RecordingRunner();
        var service = CreateService(runner);

        await Assert.ThrowsAsync<TmuxNotFoundException>(() =>
            service.KillSessionAsync("$42", CancellationToken.None));

        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task TreatsNonzeroLastSessionResultAsSuccessWhenTargetIsGone()
    {
        var runner = new RecordingRunner(
            new ProcessResult(0, "$42\n", "", TimeSpan.Zero, false, false),
            new ProcessResult(1, "", "no server running", TimeSpan.Zero, false, false),
            new ProcessResult(1, "", "no server running", TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        await service.KillSessionAsync(SafeIdentifier.ForSession("$42"), CancellationToken.None);

        Assert.Collection(runner.Requests,
            request => Assert.Equal("tmux.resolve-session", request.Operation),
            request => Assert.Equal("tmux.kill-session", request.Operation),
            request => Assert.Equal("tmux.resolve-session", request.Operation));
    }

    [Fact]
    public async Task PreservesKillFailureWhenTargetStillExists()
    {
        var runner = new RecordingRunner(
            new ProcessResult(0, "$42\n", "", TimeSpan.Zero, false, false),
            new ProcessResult(1, "", "operation failed", TimeSpan.Zero, false, false),
            new ProcessResult(0, "$42\n", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        await Assert.ThrowsAsync<TmuxCommandException>(() =>
            service.KillSessionAsync(SafeIdentifier.ForSession("$42"), CancellationToken.None));

        Assert.Equal(3, runner.Requests.Count);
    }

    private static TmuxService CreateService(IProcessRunner runner, string? socket = null) => new(
        runner,
        Options.Create(new TmuxOptions { SocketName = socket }),
        new RuleBasedSessionAnalyzer(new StatusOptions()),
        TimeProvider.System,
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
