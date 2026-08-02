using TmuxMobile.Infrastructure;

namespace TmuxMobile.Infrastructure.Tests;

public sealed class TmuxParserTests
{
    private const string S = TmuxParser.Separator;

    [Fact]
    public void MachineSeparatorIsPrintableForOlderTmuxClients() =>
        Assert.DoesNotContain(S, char.IsControl);

    [Fact]
    public void ParsesMachineDelimitedSessionsWithoutDependingOnSpaces()
    {
        var line = string.Join(S, "$1", "my session", "1700000000", "1700000010", "1", "2",
            "3", "4", "main window", "%7", "dotnet", "/home/me/a project", "pane title", "1", "1");
        var session = Assert.Single(TmuxParser.ParseSessions(line + "\n"));
        Assert.Equal("$1", session.TmuxId);
        Assert.Equal("my session", session.Name);
        Assert.Equal(2, session.Clients);
        Assert.Equal("/home/me/a project", session.WorkingDirectory);
    }

    [Fact]
    public void ParsesPanes()
    {
        var line = string.Join(S, "%2", "$1", "0", "1", "title", "bash", "/tmp", "1", "42", "100", "30");
        var pane = Assert.Single(TmuxParser.ParsePanes(line));
        Assert.True(pane.Active);
        Assert.Equal(100, pane.Width);
    }

    [Theory]
    [InlineData("too\tfew")]
    [InlineData("$1\tname\tbad\t1\t1\t1\t1\t1\tw\t%1\tc\tp\tt")]
    public void RejectsMalformedSessionOutput(string output) =>
        Assert.Throws<FormatException>(() => TmuxParser.ParseSessions(output));
}
