using TmuxCtl.Desktop;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class DesktopServerUrlTests
{
    [Theory]
    [InlineData("https://tmux.example", "https://tmux.example/desktop/")]
    [InlineData("https://tmux.example:8443/", "https://tmux.example:8443/desktop/")]
    [InlineData("http://localhost:5179", "http://localhost:5179/desktop/")]
    [InlineData("http://127.0.0.1:5179/", "http://127.0.0.1:5179/desktop/")]
    [InlineData("http://[::1]:5179", "http://[::1]:5179/desktop/")]
    public void AcceptsHttpsAndLoopbackDevelopmentHttp(string value, string expected)
    {
        Assert.True(DesktopServerUrl.TryCreate(value, out var result, out var error), error);
        Assert.Equal(expected, result!.DesktopUri.AbsoluteUri);
        Assert.Equal(new Uri(result.ServerUri, "api/desktop/capabilities"), result.CapabilitiesUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tmux.example")]
    [InlineData("http://tmux.example")]
    [InlineData("https://user:secret@tmux.example")]
    [InlineData("https://tmux.example/mobile")]
    [InlineData("https://tmux.example?token=secret")]
    [InlineData("file:///tmp/index.html")]
    public void RejectsUnsafeOrAmbiguousValues(string? value)
    {
        Assert.False(DesktopServerUrl.TryCreate(value, out var result, out var error));
        Assert.Null(result);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void NavigationUriAlwaysCarriesCacheTokenAndSafelyComposesSessionTarget()
    {
        Assert.True(DesktopServerUrl.TryCreate("https://tmux.example:8443", out var result, out var error), error);

        Assert.Equal("https://tmux.example:8443/desktop/?desktopLoad=abc123",
            result!.CreateNavigationUri("abc123").AbsoluteUri);
        Assert.Equal("https://tmux.example:8443/desktop/?desktopLoad=abc123&session=s_safe%2Ftarget",
            result.CreateNavigationUri("abc123", "s_safe/target").AbsoluteUri);
        Assert.Throws<ArgumentException>(() => result.CreateNavigationUri("not safe"));
    }
}
