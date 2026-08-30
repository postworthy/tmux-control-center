namespace TmuxCtl.Desktop;

public static class DesktopPopoutNavigation
{
    public static Uri Create(DesktopServerUrl server, string? sessionId, string cacheToken)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (!DesktopSessionTarget.IsValid(sessionId))
            throw new ArgumentException("The session target is invalid.", nameof(sessionId));
        return server.CreateNavigationUri(cacheToken, sessionId);
    }
}
