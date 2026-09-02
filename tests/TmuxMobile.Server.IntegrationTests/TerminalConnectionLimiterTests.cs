using TmuxMobile.Server;

namespace TmuxMobile.Server.IntegrationTests;

public sealed class TerminalConnectionLimiterTests
{
    [Fact]
    public void DefaultCapacitySupportsDesktopTabsAndMobileWhileRemainingBounded()
    {
        var options = new TmuxMobile.Core.SecurityOptions();
        var limiter = new TerminalConnectionLimiter();
        var leases = Enumerable.Range(0, options.MaxTerminalConnectionsPerUser)
            .Select(_ => limiter.TryAcquire("owner", options.MaxTerminalConnections,
                options.MaxTerminalConnectionsPerUser))
            .ToArray();

        Assert.All(leases, Assert.NotNull);
        Assert.Null(limiter.TryAcquire("owner", options.MaxTerminalConnections,
            options.MaxTerminalConnectionsPerUser));

        leases[0]!.Dispose();
        using var replacement = limiter.TryAcquire("owner", options.MaxTerminalConnections,
            options.MaxTerminalConnectionsPerUser);
        Assert.NotNull(replacement);

        foreach (var lease in leases.Skip(1)) lease!.Dispose();
    }
}
