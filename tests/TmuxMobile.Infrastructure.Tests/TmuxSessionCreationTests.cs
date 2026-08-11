using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class TmuxSessionCreationTests
{
    [Fact]
    public async Task UsesFixedArgumentsAndReturnsOpaqueId()
    {
        var runner = new RecordingRunner(new(0, "$42\n", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner, "mobile-test");

        var created = await service.CreateSessionAsync(" agent-1 ", CancellationToken.None);

        Assert.Equal("agent-1", created.Name);
        Assert.Equal(SafeIdentifier.ForSession("$42"), created.Id);
        var request = Assert.Single(runner.Requests);
        Assert.Equal(["-L", "mobile-test", "new-session", "-d", "-P", "-F", "#{session_id}",
            "-s", "agent-1"], request.Arguments);
        Assert.Equal("/usr/bin/tmux", request.Executable);
        Assert.Equal("tmux.create-session", request.Operation);
    }

    [Theory]
    [InlineData("bad/name")]
    [InlineData("silently.rewritten")]
    [InlineData("silently:rewritten")]
    public async Task RejectsInvalidNameBeforeStartingTmux(string name)
    {
        var runner = new RecordingRunner(new(0, "$42\n", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateSessionAsync(name, CancellationToken.None));

        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task MapsDuplicateNameWithoutReturningTmuxError()
    {
        var runner = new RecordingRunner(new(1, "", "duplicate session: work\n",
            TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        var error = await Assert.ThrowsAsync<TmuxConflictException>(() =>
            service.CreateSessionAsync("work", CancellationToken.None));

        Assert.Equal("A session with that name already exists.", error.Message);
    }

    [Fact]
    public async Task RejectsMalformedTmuxIdentifier()
    {
        var runner = new RecordingRunner(new(0, "work\n", "", TimeSpan.Zero, false, false));
        var service = CreateService(runner);

        await Assert.ThrowsAsync<TmuxCommandException>(() =>
            service.CreateSessionAsync("work", CancellationToken.None));
    }

    private static TmuxService CreateService(IProcessRunner runner, string? socket = null) => new(
        runner,
        Options.Create(new TmuxOptions { SocketName = socket }),
        new RuleBasedSessionAnalyzer(new StatusOptions()),
        TimeProvider.System,
        NullLogger<TmuxService>.Instance);

    private sealed class RecordingRunner(ProcessResult result) : IProcessRunner
    {
        public List<ProcessRequest> Requests { get; } = [];

        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(result);
        }
    }
}
