using System.Text;
using TmuxMobile.Core;

namespace TmuxMobile.Core.Tests;

public sealed class SafeIdentifierTests
{
    [Fact]
    public void CreatesStableOpaqueTypedIdentifiers()
    {
        var first = SafeIdentifier.ForSession("$12");
        Assert.Equal(first, SafeIdentifier.ForSession("$12"));
        Assert.True(SafeIdentifier.IsSession(first));
        Assert.False(SafeIdentifier.IsPane(first));
        Assert.False(SafeIdentifier.IsWindow(first));
        Assert.DoesNotContain("$12", first);

        var window = SafeIdentifier.ForWindow("@4");
        Assert.True(SafeIdentifier.IsWindow(window));
        Assert.False(SafeIdentifier.IsSession(window));
        Assert.DoesNotContain("@4", window);
    }

    [Theory]
    [InlineData("")]
    [InlineData("s_abc")]
    [InlineData("s_00000000000000000000000z")]
    [InlineData("p_000000000000000000000000")]
    public void RejectsMalformedSessionIdentifiers(string value) =>
        Assert.False(SafeIdentifier.IsSession(value));
}

public sealed class InputValidationTests
{
    [Theory]
    [InlineData("agent.1")]
    [InlineData("agent:1")]
    public void CreateRejectsCharactersTmuxWouldSilentlyRewrite(string value) =>
        Assert.Throws<ArgumentException>(() => InputValidation.ValidateCreateName(value));

    [Fact]
    public void CreateRetainsSupportedNameExactly() =>
        Assert.Equal("agent 1_@+-", InputValidation.ValidateCreateName(" agent 1_@+- "));

    [Theory]
    [InlineData(" benchmark-qwen ", "benchmark-qwen")]
    [InlineData("café", "café")]
    [InlineData("agent_1.2", "agent_1.2")]
    public void AcceptsAndNormalizesSafeRename(string input, string expected) =>
        Assert.Equal(expected, InputValidation.ValidateRename(input));

    [Theory]
    [InlineData("")]
    [InlineData(" bad/name")]
    [InlineData("bad\nname")]
    public void RejectsUnsafeRename(string value) =>
        Assert.Throws<ArgumentException>(() => InputValidation.ValidateRename(value));

    [Fact]
    public void RejectsOversizedOrNulText()
    {
        Assert.Throws<ArgumentException>(() => InputValidation.ValidateText(new string('x', 4097)));
        Assert.Throws<ArgumentException>(() => InputValidation.ValidateText("a\0b"));
    }
}

public sealed class TerminalSizeLimitsTests
{
    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(2048, 1024, true)]
    [InlineData(9, 5, false)]
    [InlineData(2049, 1024, false)]
    [InlineData(2048, 1025, false)]
    public void EnforcesBoundedHighResolutionGrid(int columns, int rows, bool expected) =>
        Assert.Equal(expected, TerminalSizeLimits.IsSupported(new(columns, rows)));
}

public sealed class StatusInferenceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");

    [Fact]
    public void FailureTakesPriority()
    {
        var analyzer = NewAnalyzer();
        var result = analyzer.Analyze(new(false, Now, "python", "fatal: failed", Now));
        Assert.Equal(SessionStatus.Failed, result.Status);
    }

    [Fact]
    public void IsConservativeWhenSignalsAreMissing()
    {
        var result = NewAnalyzer().Analyze(new(false, Now, "", "", Now));
        Assert.Equal(SessionStatus.Unknown, result.Status);
    }

    [Theory]
    [InlineData(true, "python", SessionStatus.Attached)]
    [InlineData(false, "python", SessionStatus.Active)]
    [InlineData(false, "bash", SessionStatus.Detached)]
    public void UsesMetadata(bool attached, string command, SessionStatus expected)
    {
        var result = NewAnalyzer().Analyze(new(attached, Now, command, "", Now));
        Assert.Equal(expected, result.Status);
    }

    private static RuleBasedSessionAnalyzer NewAnalyzer() => new(new StatusOptions());
}

public sealed class TerminalOutputTests
{
    [Fact]
    public void StripsAnsiAndUnsafeControlSequences()
    {
        var result = TerminalOutput.Sanitize("\u001b[31mred\u001b[0m\u001b]0;title\u0007\r\nok\0",
            1024, out var truncated);
        Assert.Equal("red\nok", result);
        Assert.False(truncated);
    }

    [Fact]
    public void TruncatesAtUtf8Boundary()
    {
        var result = TerminalOutput.Sanitize("ab😀cd", 5, out var truncated);
        Assert.Equal("ab", result);
        Assert.True(truncated);
        Assert.True(Encoding.UTF8.GetByteCount(result) <= 5);
    }
}

public sealed class TerminalKeyEncoderTests
{
    [Theory]
    [InlineData(TmuxKey.ControlC, "C-c")]
    [InlineData(TmuxKey.Enter, "Enter")]
    [InlineData(TmuxKey.TmuxPrefix, "C-a")]
    public void EncodesTmuxKeys(TmuxKey key, string expected) =>
        Assert.Equal(expected, TerminalKeyEncoder.ToTmuxKey(key, "C-a"));

    [Fact]
    public void EncodesOneShotModifiers()
    {
        Assert.Equal(new byte[] { 3 }, TerminalKeyEncoder.ToTerminalBytes("c", control: true).ToArray());
        Assert.Equal(new byte[] { 27, 120 }, TerminalKeyEncoder.ToTerminalBytes("x", alt: true).ToArray());
    }
}

public sealed class InventoryComparerTests
{
    [Fact]
    public void IgnoresOrderingButDetectsMeaningfulChange()
    {
        var a = Session("a", "one");
        var b = Session("b", "two");
        Assert.True(InventoryComparer.MeaningfullyEqual([a, b], [b, a]));
        Assert.False(InventoryComparer.MeaningfullyEqual([a], [a with { CurrentCommand = "vim" }]));
    }

    private static TmuxSession Session(string id, string name) => new(
        id, name, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, 0, 1, 1,
        "window", "pane", "bash", "/tmp", "", SessionStatus.Unknown, "", "");
}
