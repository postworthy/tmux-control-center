using System.Drawing;
using Photino.NET;

namespace TmuxCtl.Desktop;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var suppliedUrl = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("TMUXCTL_SERVER_URL");
        if (!DesktopServerUrl.TryCreate(suppliedUrl, out var server, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("Usage: tmuxctl https://your-tmuxctl-server.example");
            return 2;
        }

        var window = new PhotinoWindow()
            .SetTitle($"tmuxctl — {server!.ServerUri.Host}")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(1280, 800))
            .SetResizable(true)
            .SetContextMenuEnabled(true)
            .SetDevToolsEnabled(Environment.GetEnvironmentVariable("TMUXCTL_DEVTOOLS") == "1")
            .SetGrantBrowserPermissions(false)
            .Center()
            .Load(server.DesktopUri.AbsoluteUri);

        window.WaitForClose();
        return 0;
    }
}
