using System.Text.RegularExpressions;

namespace TmuxCtl.Desktop;

public static partial class DesktopSessionTarget
{
    public static bool IsValid(string? value) => value is not null && SessionId().IsMatch(value);

    [GeneratedRegex("^s_[a-f0-9]{24}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionId();
}
