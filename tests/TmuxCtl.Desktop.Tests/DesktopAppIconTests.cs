using TmuxCtl.Desktop;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class DesktopAppIconTests
{
    [Fact]
    public void ResolvesPublishedLinuxIconByAbsolutePath()
    {
        var directory = Directory.CreateTempSubdirectory("tmuxctl-icon-");
        try
        {
            var icon = Path.Combine(directory.FullName, DesktopAppIcon.PublishedFileName);
            File.WriteAllBytes(icon, [0x89, 0x50, 0x4e, 0x47]);

            Assert.Equal(Path.GetFullPath(icon), DesktopAppIcon.ResolveLinuxIcon(directory.FullName, true));
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public void DoesNotConfigureUnsupportedOrMissingIcon()
    {
        var directory = Directory.CreateTempSubdirectory("tmuxctl-icon-");
        try
        {
            Assert.Null(DesktopAppIcon.ResolveLinuxIcon(directory.FullName, false));
            Assert.Null(DesktopAppIcon.ResolveLinuxIcon(directory.FullName, true));
        }
        finally { directory.Delete(true); }
    }
}
