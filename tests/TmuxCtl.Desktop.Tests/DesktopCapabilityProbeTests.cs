using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using TmuxCtl.Desktop;
using TmuxMobile.Core;
using Xunit;

namespace TmuxCtl.Desktop.Tests;

public sealed class DesktopCapabilityProbeTests
{
    [Fact]
    public async Task AcceptsCurrentProtocolAndRequiredCapabilitiesFromExactEndpoint()
    {
        Uri? requested = null;
        var probe = Probe(request =>
        {
            requested = request.RequestUri;
            return Json(new DesktopCapabilities(
                DesktopProtocol.CurrentVersion,
                DesktopProtocol.MinimumSupportedClientVersion,
                DesktopProtocol.RequiredFeatures));
        });

        var result = await probe.CheckAsync(Server("https://tmux.example"));

        Assert.True(result.IsCompatible, result.Error);
        Assert.Equal("https://tmux.example/api/desktop/capabilities", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task AcceptsFutureCompatibleProtocolAndIgnoresAdditionalFieldsAndFeatures()
    {
        const string payload = """
            {"protocolVersion":2,"minimumClientProtocolVersion":1,
             "features":["session-tabs-v1","terminal-websocket-v1","tmux-topology-v1","future-v2"],
             "futureMetadata":{"value":true}}
            """;
        var result = await Probe(_ => Json(payload)).CheckAsync(Server("https://tmux.example"));
        Assert.True(result.IsCompatible, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task OlderServerReturnsActionableUpdateMessage(HttpStatusCode statusCode)
    {
        var result = await Probe(_ => new HttpResponseMessage(statusCode))
            .CheckAsync(Server("https://tmux.example"));
        Assert.False(result.IsCompatible);
        Assert.Contains("Update the tmuxctl server", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsRedirectWithoutFollowingIt()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Redirect);
        response.Headers.Location = new Uri("https://other.example/login");
        var result = await Probe(_ => response).CheckAsync(Server("https://tmux.example"));
        Assert.False(result.IsCompatible);
        Assert.Contains("canonical tmuxctl HTTPS origin", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsNewMinimumVersionOrMissingCapability()
    {
        var tooNew = await Probe(_ => Json(new DesktopCapabilities(2, 2,
                DesktopProtocol.RequiredFeatures)))
            .CheckAsync(Server("https://tmux.example"));
        Assert.False(tooNew.IsCompatible);
        Assert.Contains("incompatible", tooNew.Error, StringComparison.OrdinalIgnoreCase);

        var missing = await Probe(_ => Json(new DesktopCapabilities(1, 1,
                ["session-tabs-v1", "terminal-websocket-v1"])))
            .CheckAsync(Server("https://tmux.example"));
        Assert.False(missing.IsCompatible);
        Assert.Contains("tmux-topology-v1", missing.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsMalformedAndOversizedResponses()
    {
        var malformed = await Probe(_ => Json("{not-json"))
            .CheckAsync(Server("https://tmux.example"));
        Assert.False(malformed.IsCompatible);
        Assert.Contains("invalid desktop compatibility response", malformed.Error, StringComparison.Ordinal);

        var oversized = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[16 * 1024 + 1])
        };
        var tooLarge = await Probe(_ => oversized).CheckAsync(Server("https://tmux.example"));
        Assert.False(tooLarge.IsCompatible);
        Assert.Contains("invalid desktop compatibility response", tooLarge.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TlsFailureIsActionableWithoutLeakingExceptionText()
    {
        const string sensitive = "certificate failure for secret.internal";
        var result = await Probe(_ => throw new HttpRequestException(
                sensitive, new AuthenticationException(sensitive)))
            .CheckAsync(Server("https://tmux.example"));
        Assert.False(result.IsCompatible);
        Assert.Contains("TLS certificate", result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitive, result.Error, StringComparison.Ordinal);
    }

    private static DesktopCapabilityProbe Probe(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new HttpClient(new StubHandler(response)));

    private static HttpResponseMessage Json<T>(T value) => Json(JsonSerializer.Serialize(value));

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(value, Encoding.UTF8, "application/json")
    };

    private static DesktopServerUrl Server(string value)
    {
        Assert.True(DesktopServerUrl.TryCreate(value, out var server, out var error), error);
        return server!;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
