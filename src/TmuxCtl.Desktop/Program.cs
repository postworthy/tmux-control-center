using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Photino.NET;

namespace TmuxCtl.Desktop;

public static class Program
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(12);
    private static readonly List<DesktopWindowController> Windows = [];

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

        var capabilityProbe = new DesktopCapabilityProbe();
        var window = BuildWindow();
        var controller = new DesktopWindowController(window, profiles, capabilityProbe);
        Windows.Add(controller);
        window.RegisterWebMessageReceivedHandler(controller.HandleMessage);

        if (directServer is not null) controller.Connect(directServer);
        else controller.ShowProfiles();

        window.WaitForClose();
        controller.Dispose();
        return 0;
    }

    private static PhotinoWindow BuildWindow(PhotinoWindow? parent = null) =>
        new PhotinoWindow(parent)
            .SetTitle("tmuxctl")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(1280, 800))
            .SetResizable(true)
            .SetContextMenuEnabled(true)
            .SetDevToolsEnabled(Environment.GetEnvironmentVariable("TMUXCTL_DEVTOOLS") == "1")
            .SetGrantBrowserPermissions(false)
            .Center();

    private sealed class DesktopWindowController(
        PhotinoWindow window, ServerProfileStore profiles, DesktopCapabilityProbe capabilityProbe) : IDisposable
    {
        private static readonly JsonSerializerOptions CommandJson = new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        private bool profilesVisible;
        private int navigationGeneration;
        private DesktopServerUrl? server;
        private CancellationTokenSource? connectionCancellation;

        public void HandleMessage(object? sender, string message)
        {
            try
            {
                var command = JsonSerializer.Deserialize<ProfileCommand>(message, CommandJson)
                    ?? throw new InvalidDataException("The desktop command is empty.");
                if (!profilesVisible && command.Type is not ("showProfiles" or "desktopReady" or "openSessionWindow"))
                    throw new InvalidDataException("Profile changes are available only from the native server chooser.");
                switch (command.Type)
                {
                    case "showProfiles": ShowProfiles(); break;
                    case "desktopReady": MarkDesktopReady(); break;
                    case "openSessionWindow": OpenSessionWindow(command.SessionId); break;
                    case "connect":
                    {
                        var id = ParseId(command.Id);
                        var profile = profiles.Load().SingleOrDefault(item => item.Id == id)
                                      ?? throw new InvalidDataException("That server profile no longer exists.");
                        if (!DesktopServerUrl.TryCreate(profile.ServerUrl, out var selected, out var error))
                            throw new InvalidDataException(error);
                        Connect(selected!);
                        break;
                    }
                    case "saveAndConnect":
                    {
                        Guid? id = string.IsNullOrWhiteSpace(command.Id) ? null : ParseId(command.Id);
                        var profile = profiles.Save(id, command.Label, command.Url);
                        DesktopServerUrl.TryCreate(profile.ServerUrl, out var selected, out _);
                        Connect(selected!);
                        break;
                    }
                    case "delete":
                        profiles.Delete(ParseId(command.Id));
                        ShowProfiles();
                        break;
                    default: throw new InvalidDataException("The desktop command is unsupported.");
                }
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidDataException or
                                              IOException or UnauthorizedAccessException or JsonException)
            {
                ShowProfiles(exception.Message);
            }
        }

        public void Connect(DesktopServerUrl selected, string? sessionId = null)
        {
            CancelPendingConnection();
            profilesVisible = false;
            server = selected;
            var generation = Interlocked.Increment(ref navigationGeneration);
            connectionCancellation = new CancellationTokenSource();
            window.SetTitle($"tmuxctl — {selected.ServerUri.Host}");
            window.LoadRawString(ProfileChooserPage.RenderConnecting(selected.ServerUri.Host));
            _ = NavigateAfterCapabilityCheckAsync(
                generation, selected, sessionId, connectionCancellation.Token);
        }

        private void ConnectKnownCompatible(DesktopServerUrl selected, string sessionId)
        {
            CancelPendingConnection();
            profilesVisible = false;
            server = selected;
            Interlocked.Increment(ref navigationGeneration);
            window.SetTitle($"tmuxctl — {selected.ServerUri.Host}");
            window.Load(DesktopPopoutNavigation.Create(
                selected, sessionId, Guid.NewGuid().ToString("N")).AbsoluteUri);
        }

        private async Task NavigateAfterCapabilityCheckAsync(
            int generation, DesktopServerUrl selected, string? sessionId, CancellationToken cancellationToken)
        {
            DesktopCapabilityCheck check;
            try { check = await capabilityProbe.CheckAsync(selected, cancellationToken); }
            catch (OperationCanceledException) { return; }
            if (cancellationToken.IsCancellationRequested) return;
            window.Invoke(() =>
            {
                if (cancellationToken.IsCancellationRequested ||
                    Volatile.Read(ref navigationGeneration) != generation) return;
                if (!check.IsCompatible)
                {
                    ShowProfiles(check.Error);
                    return;
                }

                var uri = selected.CreateNavigationUri(Guid.NewGuid().ToString("N"), sessionId);
                window.Load(uri.AbsoluteUri);
                _ = ReturnToProfilesOnConnectionTimeoutAsync(generation, selected.ServerUri.Host);
            });
        }

        public void ShowProfiles(string? error = null)
        {
            CancelPendingConnection();
            profilesVisible = true;
            server = null;
            Interlocked.Increment(ref navigationGeneration);
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

        private void OpenSessionWindow(string? sessionId)
        {
            if (server is null) throw new InvalidDataException("A server connection is required.");
            if (!DesktopSessionTarget.IsValid(sessionId))
                throw new InvalidDataException("The session target is invalid.");
            var childWindow = BuildWindow(window);
            var child = new DesktopWindowController(childWindow, profiles, capabilityProbe);
            Windows.Add(child);
            childWindow.RegisterWebMessageReceivedHandler(child.HandleMessage);
            child.ConnectKnownCompatible(server, sessionId!);
            childWindow.WaitForClose();
            child.Dispose();
            Windows.Remove(child);
        }

        private void MarkDesktopReady()
        {
            Interlocked.Increment(ref navigationGeneration);
            CancelPendingConnection();
        }

        private void CancelPendingConnection()
        {
            connectionCancellation?.Cancel();
            connectionCancellation?.Dispose();
            connectionCancellation = null;
        }

        private async Task ReturnToProfilesOnConnectionTimeoutAsync(int generation, string host)
        {
            await Task.Delay(ConnectionTimeout);
            window.Invoke(() =>
            {
                if (!profilesVisible && Volatile.Read(ref navigationGeneration) == generation)
                    ShowProfiles($"Could not connect to {host}. Check the server, network, and TLS certificate, then try again.");
            });
        }

        private static Guid ParseId(string? value) =>
            Guid.TryParse(value, out var id) && id != Guid.Empty
                ? id
                : throw new InvalidDataException("The server profile ID is invalid.");

        public void Dispose() => CancelPendingConnection();
    }

    private sealed record ProfileCommand(
        string Type, string? Id, string? Label, string? Url, string? SessionId);
}
