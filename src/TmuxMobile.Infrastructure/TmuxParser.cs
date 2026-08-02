using TmuxMobile.Core;

namespace TmuxMobile.Infrastructure;

public sealed record RawSession(
    string TmuxId, string Name, long CreatedUnix, long ActivityUnix, bool Attached,
    int Clients, int Windows, int Panes, string WindowName, string PaneTmuxId,
    string Command, string WorkingDirectory, string Title, bool WindowActive, bool PaneActive);

public sealed record RawPane(
    string TmuxId, string SessionTmuxId, int WindowIndex, int PaneIndex, string Title,
    string Command, string WorkingDirectory, bool Active, int ProcessId, int Width, int Height);

public static class TmuxParser
{
    public const string Separator = "|:tmux-mobile:|";

    public static IReadOnlyList<RawSession> ParseSessions(string output)
    {
        var result = new List<RawSession>();
        foreach (var line in Lines(output))
        {
            var fields = line.Split(Separator, StringSplitOptions.None);
            if (fields.Length != 15) throw new FormatException($"Expected 15 session fields, received {fields.Length}.");
            result.Add(new RawSession(fields[0], fields[1], Long(fields[2], "created"),
                Long(fields[3], "activity"), Bool(fields[4]), Int(fields[5], "clients"),
                Int(fields[6], "windows"), Int(fields[7], "panes"), fields[8], fields[9],
                fields[10], fields[11], fields[12], Bool(fields[13]), Bool(fields[14])));
        }
        return result;
    }

    public static IReadOnlyList<RawPane> ParsePanes(string output)
    {
        var result = new List<RawPane>();
        foreach (var line in Lines(output))
        {
            var fields = line.Split(Separator, StringSplitOptions.None);
            if (fields.Length != 11) throw new FormatException($"Expected 11 pane fields, received {fields.Length}.");
            result.Add(new RawPane(fields[0], fields[1], Int(fields[2], "window index"),
                Int(fields[3], "pane index"), fields[4], fields[5], fields[6], Bool(fields[7]),
                Int(fields[8], "pid"), Int(fields[9], "width"), Int(fields[10], "height")));
        }
        return result;
    }

    private static IEnumerable<string> Lines(string output) =>
        output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
    private static int Int(string value, string field) =>
        int.TryParse(value, out var result) ? result : throw new FormatException($"Invalid {field}.");
    private static long Long(string value, string field) =>
        long.TryParse(value, out var result) ? result : throw new FormatException($"Invalid {field}.");
    private static bool Bool(string value) => value == "1" ? true :
        value == "0" ? false : throw new FormatException("Invalid boolean field.");
}
