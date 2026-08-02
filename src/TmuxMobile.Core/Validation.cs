using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TmuxMobile.Core;

public static partial class SafeIdentifier
{
    private const string SessionPrefix = "s_";
    private const string PanePrefix = "p_";

    public static string ForSession(string tmuxId) => Encode(SessionPrefix, tmuxId);
    public static string ForPane(string tmuxId) => Encode(PanePrefix, tmuxId);

    public static bool IsSession(string value) => IsValid(value, SessionPrefix);
    public static bool IsPane(string value) => IsValid(value, PanePrefix);

    private static string Encode(string prefix, string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return prefix + Convert.ToHexString(bytes[..12]).ToLowerInvariant();
    }

    private static bool IsValid(string? value, string prefix) =>
        value is not null && value.StartsWith(prefix, StringComparison.Ordinal) &&
        value.Length == prefix.Length + 24 && HexRegex().IsMatch(value[prefix.Length..]);

    [GeneratedRegex("^[a-f0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex HexRegex();
}

public static partial class InputValidation
{
    public static string ValidateRename(string? name)
    {
        var normalized = name?.Normalize(NormalizationForm.FormC).Trim() ?? "";
        if (normalized.Length is < 1 or > 64)
            throw new ArgumentException("Session name must contain 1 to 64 characters.");
        if (!RenameRegex().IsMatch(normalized))
            throw new ArgumentException("Session name contains unsupported characters.");
        return normalized;
    }

    public static string ValidateText(string? text, int maxLength = 4096)
    {
        if (text is null) throw new ArgumentException("Text is required.");
        if (text.Length > maxLength) throw new ArgumentException($"Text may not exceed {maxLength} characters.");
        if (text.IndexOf('\0') >= 0) throw new ArgumentException("Text may not contain NUL.");
        return text;
    }

    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{N} ._:@+\-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex RenameRegex();
}
