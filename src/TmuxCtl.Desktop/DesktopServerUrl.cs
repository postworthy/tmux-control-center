using System.Net;

namespace TmuxCtl.Desktop;

public sealed record DesktopServerUrl(Uri ServerUri, Uri DesktopUri)
{
    public Uri CapabilitiesUri => new(ServerUri, "api/desktop/capabilities");

    public Uri CreateNavigationUri(string cacheToken, string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(cacheToken) ||
            cacheToken.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new ArgumentException("The desktop cache token must be alphanumeric.", nameof(cacheToken));

        var query = $"desktopLoad={Uri.EscapeDataString(cacheToken)}";
        if (sessionId is not null)
            query += $"&session={Uri.EscapeDataString(sessionId)}";
        return new UriBuilder(DesktopUri) { Query = query }.Uri;
    }

    public static bool TryCreate(string? value, out DesktopServerUrl? result, out string error)
    {
        result = null;
        error = "";
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var candidate))
        {
            error = "The server URL must be an absolute HTTPS URL.";
            return false;
        }

        var isHttps = candidate.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isLoopbackDevelopmentHttp = candidate.Scheme.Equals(Uri.UriSchemeHttp,
                                                StringComparison.OrdinalIgnoreCase) &&
                                        IsLoopbackHost(candidate.Host);
        if (!isHttps && !isLoopbackDevelopmentHttp)
        {
            error = "Use HTTPS. Plain HTTP is accepted only for localhost development.";
            return false;
        }
        if (!string.IsNullOrEmpty(candidate.UserInfo))
        {
            error = "The server URL cannot contain credentials.";
            return false;
        }
        if (!string.IsNullOrEmpty(candidate.Query) || !string.IsNullOrEmpty(candidate.Fragment))
        {
            error = "The server URL cannot contain a query or fragment.";
            return false;
        }
        if (candidate.AbsolutePath is not ("" or "/"))
        {
            error = "Enter the tmuxctl server origin without an application path.";
            return false;
        }

        var origin = new UriBuilder(candidate.Scheme, candidate.Host, candidate.IsDefaultPort ? -1 : candidate.Port)
        {
            Path = "/"
        }.Uri;
        result = new DesktopServerUrl(origin, new Uri(origin, "desktop/"));
        return true;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
}
