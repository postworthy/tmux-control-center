using TmuxCtl.Desktop;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class DesktopSessionTargetTests
{
    [Theory]
    [InlineData("s_0123456789abcdef01234567", true)]
    [InlineData("$1", false)]
    [InlineData("s_0123456789abcdef0123456z", false)]
    [InlineData("s_0123456789abcdef01234567?command=run-shell", false)]
    [InlineData(null, false)]
    public void AcceptsOnlyOpaqueSessionIdentifiers(string? value, bool expected) =>
        Assert.Equal(expected, DesktopSessionTarget.IsValid(value));
}
