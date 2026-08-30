namespace TmuxCtl.Desktop;

public static class DesktopAppIcon
{
    public const string PublishedFileName = "tmuxctl.png";

    public static string? ResolveLinuxIcon(string baseDirectory, bool isLinux)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        if (!isLinux) return null;
        var icon = Path.GetFullPath(Path.Combine(baseDirectory, PublishedFileName));
        return File.Exists(icon) ? icon : null;
    }
}
