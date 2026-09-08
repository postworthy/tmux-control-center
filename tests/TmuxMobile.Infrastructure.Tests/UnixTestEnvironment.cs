namespace TmuxMobile.Infrastructure.Tests;

internal static class UnixTestEnvironment
{
    public static string TmuxExecutable
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("TMUX_MOBILE_TEST_TMUX");
            var path = configured ?? (OperatingSystem.IsMacOS() ? "/opt/homebrew/bin/tmux" : "/usr/bin/tmux");
            if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
                throw new InvalidOperationException("Set TMUX_MOBILE_TEST_TMUX to an existing absolute tmux executable path.");
            return path;
        }
    }
}

public sealed class UnixIntegrationFactAttribute : FactAttribute
{
    public UnixIntegrationFactAttribute()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            Skip = "Unix PTY integration requires Linux or macOS.";
        else if (Environment.GetEnvironmentVariable("TMUX_MOBILE_RUN_UNIX_INTEGRATION") != "1" &&
                 Environment.GetEnvironmentVariable("TMUX_MOBILE_RUN_LINUX_INTEGRATION") != "1")
            Skip = "Set TMUX_MOBILE_RUN_UNIX_INTEGRATION=1 to run isolated native tmux tests.";
    }
}
