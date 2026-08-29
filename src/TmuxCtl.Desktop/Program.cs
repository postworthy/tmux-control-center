using System.Drawing;
using System.Text.Json;
using Photino.NET;

namespace TmuxCtl.Desktop;

public static class Program
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(12);
    private static bool _profilesVisible;
    private static int _navigationGeneration;

    [STAThread]
    public static int Main(string[] args)
    {
        var suppliedUrl = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("TMUXCTL_SERVER_URL");
        DesktopServerUrl? directServer = null;
        if (!string.IsNullOrWhiteSpace(suppliedUrl) &&
            !DesktopServerUrl.TryCreate(suppliedUrl, out directServer, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("Usage: tmuxctl https://your-tmuxctl-server.example");
            return 2;
        }

        ServerProfileStore profiles;
        try { profiles = new ServerProfileStore(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine(exception.Message);
            return 3;
        }

        var window = new PhotinoWindow()
            .SetTitle("tmuxctl")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(1280, 800))
            .SetResizable(true)
            .SetContextMenuEnabled(true)
            .SetDevToolsEnabled(Environment.GetEnvironmentVariable("TMUXCTL_DEVTOOLS") == "1")
            .SetGrantBrowserPermissions(false)
            .Center()
            .RegisterWebMessageReceivedHandler((sender, message) =>
                HandleMessage((PhotinoWindow)sender!, profiles, message));

        if (directServer is not null) Connect(window, profiles, directServer);
        else ShowProfiles(window, profiles);

        window.WaitForClose();
        return 0;
    }

    private static void HandleMessage(PhotinoWindow window, ServerProfileStore profiles, string message)
    {
        try
        {
            var command = JsonSerializer.Deserialize<ProfileCommand>(message,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("The desktop command is empty.");
            if (!_profilesVisible && command.Type is not ("showProfiles" or "desktopReady"))
                throw new InvalidDataException("Profile changes are available only from the native server chooser.");
            switch (command.Type)
            {
                case "showProfiles":
                    ShowProfiles(window, profiles);
                    break;
                case "desktopReady":
                    Interlocked.Increment(ref _navigationGeneration);
                    break;
                case "connect":
                {
                    var id = ParseId(command.Id);
                    var profile = profiles.Load().SingleOrDefault(item => item.Id == id)
                                  ?? throw new InvalidDataException("That server profile no longer exists.");
                    if (!DesktopServerUrl.TryCreate(profile.ServerUrl, out var server, out var error))
                        throw new InvalidDataException(error);
                    Connect(window, profiles, server!);
                    break;
                }
                case "saveAndConnect":
                {
                    Guid? id = string.IsNullOrWhiteSpace(command.Id) ? null : ParseId(command.Id);
                    var profile = profiles.Save(id, command.Label, command.Url);
                    DesktopServerUrl.TryCreate(profile.ServerUrl, out var server, out _);
                    Connect(window, profiles, server!);
                    break;
                }
                case "delete":
                    profiles.Delete(ParseId(command.Id));
                    ShowProfiles(window, profiles);
                    break;
                default:
                    throw new InvalidDataException("The desktop command is unsupported.");
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            ShowProfiles(window, profiles, exception.Message);
        }
    }

    private static Guid ParseId(string? value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidDataException("The server profile ID is invalid.");

    private static void Connect(PhotinoWindow window, ServerProfileStore profiles, DesktopServerUrl server)
    {
        _profilesVisible = false;
        var generation = Interlocked.Increment(ref _navigationGeneration);
        window.SetTitle($"tmuxctl — {server.ServerUri.Host}");
        window.Load(server.DesktopUri.AbsoluteUri);
        _ = ReturnToProfilesOnConnectionTimeoutAsync(window, profiles, generation, server.ServerUri.Host);
    }

    private static async Task ReturnToProfilesOnConnectionTimeoutAsync(
        PhotinoWindow window, ServerProfileStore profiles, int generation, string host)
    {
        await Task.Delay(ConnectionTimeout);
        window.Invoke(() =>
        {
            if (!_profilesVisible && Volatile.Read(ref _navigationGeneration) == generation)
                ShowProfiles(window, profiles, $"Could not connect to {host}. Check the server, network, and TLS certificate, then try again.");
        });
    }

    private static void ShowProfiles(PhotinoWindow window, ServerProfileStore profiles, string? error = null)
    {
        _profilesVisible = true;
        Interlocked.Increment(ref _navigationGeneration);
        IReadOnlyList<ServerProfile> saved;
        try { saved = profiles.Load(); }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            saved = [];
            error = exception.Message;
        }
        window.SetTitle("tmuxctl — Servers");
        window.LoadRawString(ProfileChooserPage.Render(saved, error));
    }

    private sealed record ProfileCommand(string Type, string? Id, string? Label, string? Url);
}
