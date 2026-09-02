using System.Text.Json;

namespace TmuxCtl.Desktop;

public sealed record ServerProfile(Guid Id, string Label, string ServerUrl);

public sealed class ServerProfileStore
{
    private const int MaxSettingsBytes = 1_048_576;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _path;

    public ServerProfileStore(string? path = null)
    {
        _path = path ?? DefaultPath();
        if (!System.IO.Path.IsPathFullyQualified(_path))
            throw new ArgumentException("The profile store path must be absolute.", nameof(path));
    }

    public string Path => _path;

    public IReadOnlyList<ServerProfile> Load()
    {
        if (!File.Exists(_path)) return [];
        var info = new FileInfo(_path);
        if (info.Length > MaxSettingsBytes)
            throw new InvalidDataException("The tmuxctl profile store is too large.");
        try
        {
            var document = JsonSerializer.Deserialize<ProfileDocument>(File.ReadAllText(_path), JsonOptions)
                           ?? throw new InvalidDataException("The tmuxctl profile store is empty.");
            if (document.Version != 1)
                throw new InvalidDataException("The tmuxctl profile store version is unsupported.");
            var profiles = document.Profiles.Select(ValidateStoredProfile).ToArray();
            if (profiles.Select(profile => profile.Id).Distinct().Count() != profiles.Length)
                throw new InvalidDataException("The tmuxctl profile store contains duplicate IDs.");
            return profiles;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The tmuxctl profile store is not valid JSON.", exception);
        }
    }

    public ServerProfile Save(Guid? id, string? label, string? serverUrl)
    {
        var normalizedLabel = ValidateLabel(label);
        if (!DesktopServerUrl.TryCreate(serverUrl, out var parsed, out var error))
            throw new ArgumentException(error, nameof(serverUrl));

        var profiles = Load().ToList();
        var profileId = id is { } supplied && supplied != Guid.Empty ? supplied : Guid.NewGuid();
        var profile = new ServerProfile(profileId, normalizedLabel, parsed!.ServerUri.AbsoluteUri.TrimEnd('/'));
        var existingIndex = profiles.FindIndex(item => item.Id == profileId);
        if (existingIndex >= 0) profiles[existingIndex] = profile;
        else profiles.Add(profile);
        Write(profiles);
        return profile;
    }

    public bool Delete(Guid id)
    {
        var profiles = Load().ToList();
        var removed = profiles.RemoveAll(profile => profile.Id == id) != 0;
        if (removed) Write(profiles);
        return removed;
    }

    private void Write(IReadOnlyList<ServerProfile> profiles)
    {
        var directory = System.IO.Path.GetDirectoryName(_path)
                        ?? throw new InvalidOperationException("Profile store directory is unavailable.");
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var temporary = System.IO.Path.Combine(directory, $".{System.IO.Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(new ProfileDocument(1, profiles), JsonOptions);
            File.WriteAllText(temporary, json);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporary, _path, true);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static ServerProfile ValidateStoredProfile(ServerProfile profile)
    {
        if (profile.Id == Guid.Empty) throw new InvalidDataException("A tmuxctl profile ID is empty.");
        string label;
        try { label = ValidateLabel(profile.Label); }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("A stored tmuxctl profile label is invalid.", exception);
        }
        if (!DesktopServerUrl.TryCreate(profile.ServerUrl, out var parsed, out _))
            throw new InvalidDataException("A stored tmuxctl profile URL is invalid.");
        return new ServerProfile(profile.Id, label, parsed!.ServerUri.AbsoluteUri.TrimEnd('/'));
    }

    private static string ValidateLabel(string? value)
    {
        var label = value?.Trim() ?? "";
        if (label.Length is < 1 or > 80 || label.Any(char.IsControl))
            throw new ArgumentException("Profile labels must contain 1-80 visible characters.", nameof(value));
        return label;
    }

    private static string DefaultPath()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("TMUXCTL_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            if (!System.IO.Path.IsPathFullyQualified(overrideDirectory))
                throw new InvalidOperationException("TMUXCTL_CONFIG_HOME must be an absolute path.");
            return System.IO.Path.Combine(overrideDirectory, "profiles.json");
        }
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(applicationData))
            throw new InvalidOperationException("The operating system did not provide an application settings directory.");
        return System.IO.Path.Combine(applicationData, "tmuxctl", "profiles.json");
    }

    private sealed record ProfileDocument(int Version, IReadOnlyList<ServerProfile> Profiles);
}
