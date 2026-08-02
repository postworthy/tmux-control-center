using System.Text.RegularExpressions;

namespace TmuxMobile.Core;

public sealed class RuleBasedSessionAnalyzer(StatusOptions options) : ISessionAnalyzer
{
    public (SessionStatus Status, string Reason) Analyze(SessionAnalysisInput input)
    {
        var text = input.PreviewText;
        if (Matches(text, options.FailurePatterns))
            return (SessionStatus.Failed, "Recent output matches a configured failure pattern.");
        if (Matches(text, options.WaitingPatterns))
            return (SessionStatus.Waiting, "Recent output appears to be waiting for input.");
        if (Matches(text, options.CompletedPatterns))
            return (SessionStatus.Completed, "Recent output matches a configured completion pattern.");
        if (input.IsAttached)
            return (SessionStatus.Attached, "A tmux client is attached.");
        if (input.Now - input.LastActivityAt >= TimeSpan.FromMinutes(options.IdleAfterMinutes))
            return (SessionStatus.Idle, "No recent tmux activity.");
        if (!string.IsNullOrWhiteSpace(input.CurrentCommand) &&
            !options.ShellCommands.Contains(input.CurrentCommand, StringComparer.OrdinalIgnoreCase))
            return (SessionStatus.Active, "A foreground process is running.");
        if (options.ShellCommands.Contains(input.CurrentCommand, StringComparer.OrdinalIgnoreCase))
            return (SessionStatus.Detached, "Detached session is at a shell.");
        return (SessionStatus.Unknown, "Not enough evidence to infer status.");
    }

    private static bool Matches(string text, IEnumerable<string> patterns) =>
        patterns.Any(pattern => !string.IsNullOrWhiteSpace(pattern) &&
            Regex.IsMatch(text, Regex.Escape(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)));
}

public static partial class TerminalOutput
{
    public static string Sanitize(string input, int maxBytes, out bool truncated)
    {
        input ??= "";
        var withoutOsc = OscRegex().Replace(input, "");
        var withoutCsi = CsiRegex().Replace(withoutOsc, "");
        var cleaned = ControlRegex().Replace(withoutCsi, "");
        var bytes = System.Text.Encoding.UTF8.GetBytes(cleaned);
        truncated = bytes.Length > maxBytes;
        if (!truncated) return cleaned.Replace("\r\n", "\n").Replace('\r', '\n');

        var length = maxBytes;
        while (length > 0 && (bytes[length] & 0xC0) == 0x80) length--;
        return System.Text.Encoding.UTF8.GetString(bytes, 0, length)
            .Replace("\r\n", "\n").Replace('\r', '\n');
    }

    [GeneratedRegex(@"\x1B\][^\x07]*(?:\x07|\x1B\\)", RegexOptions.CultureInvariant)]
    private static partial Regex OscRegex();
    [GeneratedRegex(@"\x1B(?:\[[0-?]*[ -/]*[@-~]|\([A-Z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex CsiRegex();
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.CultureInvariant)]
    private static partial Regex ControlRegex();
}

public static class TerminalKeyEncoder
{
    public static string ToTmuxKey(TmuxKey key, string prefix = "C-b") => key switch
    {
        TmuxKey.Enter => "Enter",
        TmuxKey.Escape => "Escape",
        TmuxKey.Tab => "Tab",
        TmuxKey.Up => "Up",
        TmuxKey.Down => "Down",
        TmuxKey.Left => "Left",
        TmuxKey.Right => "Right",
        TmuxKey.Backspace => "BSpace",
        TmuxKey.Delete => "DC",
        TmuxKey.Home => "Home",
        TmuxKey.End => "End",
        TmuxKey.PageUp => "PPage",
        TmuxKey.PageDown => "NPage",
        TmuxKey.ControlC => "C-c",
        TmuxKey.ControlD => "C-d",
        TmuxKey.TmuxPrefix => prefix,
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };

    public static ReadOnlyMemory<byte> ToTerminalBytes(string key, bool control = false, bool alt = false)
    {
        var value = key switch
        {
            "Escape" => "\u001b",
            "Tab" => "\t",
            "Enter" => "\r",
            "Up" => "\u001b[A",
            "Down" => "\u001b[B",
            "Right" => "\u001b[C",
            "Left" => "\u001b[D",
            _ when key.Length == 1 => key,
            _ => throw new ArgumentException("Unsupported terminal key.", nameof(key))
        };
        if (control && value.Length == 1)
        {
            var c = char.ToUpperInvariant(value[0]);
            if (c is >= '@' and <= '_') value = ((char)(c & 0x1f)).ToString();
        }
        if (alt) value = "\u001b" + value;
        return System.Text.Encoding.UTF8.GetBytes(value);
    }
}

public static class InventoryComparer
{
    public static bool MeaningfullyEqual(IReadOnlyList<TmuxSession> left, IReadOnlyList<TmuxSession> right) =>
        left.Count == right.Count && left.OrderBy(x => x.Id).SequenceEqual(right.OrderBy(x => x.Id));
}
