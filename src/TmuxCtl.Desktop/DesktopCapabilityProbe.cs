using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.Json;
using TmuxMobile.Core;

namespace TmuxCtl.Desktop;

public sealed record DesktopCapabilityCheck(bool IsCompatible, string? Error)
{
    public static DesktopCapabilityCheck Compatible { get; } = new(true, null);
    public static DesktopCapabilityCheck Incompatible(string error) => new(false, error);
}

public sealed class DesktopCapabilityProbe
{
    private const int MaximumResponseBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 8
    };
    private readonly HttpClient client;

    public DesktopCapabilityProbe() : this(CreateClient()) { }

    public DesktopCapabilityProbe(HttpClient client) =>
        this.client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<DesktopCapabilityCheck> CheckAsync(
        DesktopServerUrl server, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, server.CapabilitiesUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized)
                return DesktopCapabilityCheck.Incompatible(
                    "This server does not support tmuxctl desktop protocol 1. Update the tmuxctl server and try again.");
            if ((int)response.StatusCode is >= 300 and < 400)
                return DesktopCapabilityCheck.Incompatible(
                    "The server redirected the desktop compatibility check. Enter the canonical tmuxctl HTTPS origin.");
            if (!response.IsSuccessStatusCode)
                return DesktopCapabilityCheck.Incompatible(
                    $"The server compatibility check failed with HTTP {(int)response.StatusCode}. Check the server and try again.");
            if (response.Content.Headers.ContentLength is > MaximumResponseBytes)
                return InvalidResponse();

            var payload = await ReadBoundedAsync(response.Content, cancellationToken);
            if (payload is null) return InvalidResponse();
            DesktopCapabilities? capabilities;
            try { capabilities = JsonSerializer.Deserialize<DesktopCapabilities>(payload, JsonOptions); }
            catch (JsonException) { return InvalidResponse(); }
            if (capabilities is null || capabilities.ProtocolVersion < DesktopProtocol.CurrentVersion ||
                capabilities.MinimumClientProtocolVersion > DesktopProtocol.CurrentVersion ||
                capabilities.MinimumClientProtocolVersion < 1 || capabilities.Features is null)
                return DesktopCapabilityCheck.Incompatible(
                    "This server uses an incompatible tmuxctl desktop protocol. Update tmuxctl on the desktop and server.");

            var features = capabilities.Features.ToHashSet(StringComparer.Ordinal);
            var missing = DesktopProtocol.RequiredFeatures.Where(feature => !features.Contains(feature)).ToArray();
            return missing.Length == 0
                ? DesktopCapabilityCheck.Compatible
                : DesktopCapabilityCheck.Incompatible(
                    $"This server is missing required desktop capability: {string.Join(", ", missing)}. Update the tmuxctl server.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DesktopCapabilityCheck.Incompatible(
                "The server compatibility check timed out. Check the server, network, and Tailscale connection.");
        }
        catch (HttpRequestException exception)
        {
            var tlsFailure = exception.InnerException is AuthenticationException;
            return DesktopCapabilityCheck.Incompatible(tlsFailure
                ? "The server TLS certificate could not be verified. Check the certificate and tmuxctl HTTPS URL."
                : "The server compatibility check could not connect. Check the server, network, TLS, and Tailscale connection.");
        }
        catch (IOException)
        {
            return DesktopCapabilityCheck.Incompatible(
                "The server compatibility response could not be read. Check the server and network, then try again.");
        }
    }

    private static DesktopCapabilityCheck InvalidResponse() => DesktopCapabilityCheck.Incompatible(
        "The server returned an invalid desktop compatibility response. Update the tmuxctl server and try again.");

    private static async Task<byte[]?> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var bytes = new byte[MaximumResponseBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = await stream.ReadAsync(bytes.AsMemory(total, bytes.Length - total), cancellationToken);
            if (read == 0) break;
            total += read;
        }
        return total > MaximumResponseBytes ? null : bytes[..total];
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }
}
