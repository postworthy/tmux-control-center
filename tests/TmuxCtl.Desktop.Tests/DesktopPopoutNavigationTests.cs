using TmuxCtl.Desktop;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class DesktopPopoutNavigationTests
{
    [Fact]
    public void CreatesKnownCompatibleCacheBustedSessionNavigation()
    {
        Assert.True(DesktopServerUrl.TryCreate("https://tmux.example:8443", out var server, out _));

        var uri = DesktopPopoutNavigation.Create(
            server!, "s_0123456789abcdef01234567", "abc123");

        Assert.Equal(
            "https://tmux.example:8443/desktop/?desktopLoad=abc123&session=s_0123456789abcdef01234567",
            uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("$1")]
    [InlineData("s_0123456789abcdef01234567?command=run-shell")]
    [InlineData(null)]
    public void RefusesUntrustedSessionTargets(string? sessionId)
    {
        Assert.True(DesktopServerUrl.TryCreate("https://tmux.example", out var server, out _));
        Assert.Throws<ArgumentException>(() =>
            DesktopPopoutNavigation.Create(server!, sessionId, "abc123"));
    }
}
