using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TmuxMobile.Core;
using TmuxMobile.Server;

namespace TmuxMobile.Server.IntegrationTests;

public sealed class ApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task InventoryEndpointsUseDomainModels()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient();
        var sessions = await client.GetFromJsonAsync<TmuxSession[]>("/api/sessions", JsonOptions);
        var session = Assert.Single(sessions!);
        Assert.Equal("work", session.Name);

        var panes = await client.GetFromJsonAsync<TmuxPane[]>($"/api/sessions/{session.Id}/panes");
        Assert.Single(panes!);
        var capture = await client.GetFromJsonAsync<CaptureResponse>(
            $"/api/panes/{session.CurrentPaneId}/capture?lines=99999");
        Assert.Equal("safe <text>", capture!.Text);
        Assert.Equal(500, capture.RequestedLines);
    }

    [Fact]
    public async Task InvalidTargetsReturnNotFound()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/sessions/s_000000000000000000000000")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/panes/p_000000000000000000000000/capture")).StatusCode);
    }

    [Fact]
    public async Task ProductionApiRequiresAuthentication()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var response = await CreateHttpsClient(factory).GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WorkspaceRecoveryStatusIsAuthenticatedAndDisabledByDefault()
    {
        await using var production = new TmuxFactory(authenticated: false);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await CreateHttpsClient(production).GetAsync("/api/workspace-recovery")).StatusCode);

        await using var development = new TmuxFactory(authenticated: true);
        var status = await development.CreateClient()
            .GetFromJsonAsync<JsonElement>("/api/workspace-recovery");
        Assert.False(status.GetProperty("enabled").GetBoolean());
        Assert.Equal("disabled", status.GetProperty("state").GetString());
    }

    [Fact]
    public async Task WorkspaceRestoreRequiresEmptyTmuxAndCreatesOnlyFixedRequest()
    {
        await using var liveFactory = new TmuxFactory(authenticated: true,
            workspaceRecovery: true);
        liveFactory.WriteRecoverySnapshot();
        var liveClient = liveFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var blocked = await SendWithCsrfAsync(liveClient, HttpMethod.Post,
            "/api/workspace-recovery/restore", new { });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.False(File.Exists(liveFactory.RestoreRequestPath));

        await using var emptyFactory = new TmuxFactory(authenticated: true,
            workspaceRecovery: true, emptySessions: true);
        emptyFactory.WriteRecoverySnapshot();
        var emptyClient = emptyFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var accepted = await SendWithCsrfAsync(emptyClient, HttpMethod.Post,
            "/api/workspace-recovery/restore", new { });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var fields = (await File.ReadAllTextAsync(emptyFactory.RestoreRequestPath)).Trim().Split('\t');
        Assert.Equal(3, fields.Length);
        Assert.Equal("1", fields[0]);
        Assert.True(Guid.TryParseExact(fields[1], "D", out _));
        Assert.True(long.TryParse(fields[2], out _));
        if (OperatingSystem.IsLinux())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite,
                File.GetUnixFileMode(emptyFactory.RestoreRequestPath));
        Assert.Contains(emptyFactory.AuditRecords,
            record => record.Action == "workspace.restore.request" && record.Succeeded);
    }

    [Fact]
    public async Task WorkspaceRestoreRequiresCsrfAndSnapshot()
    {
        await using var factory = new TmuxFactory(authenticated: true,
            workspaceRecovery: true, emptySessions: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/workspace-recovery/restore", new { })).StatusCode);
        var missing = await SendWithCsrfAsync(client, HttpMethod.Post,
            "/api/workspace-recovery/restore", new { });
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "workspace.restore.request" && !record.Succeeded);
    }

    [Fact]
    public async Task AnonymousHealthCanOnlyUseLiveness()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = CreateHttpsClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task LoginAttemptsHaveIndependentRemotePartitionLimit()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = CreateHttpsClient(factory);
        for (var attempt = 0; attempt < 10; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
                new { apiKey = "invalid-access-key" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login",
            new { apiKey = "invalid-access-key" })).StatusCode);
    }

    [Fact]
    public async Task AnonymousAppShellCanLoadEveryLinkedAsset()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = CreateHttpsClient(factory);
        var html = await client.GetStringAsync("/");
        var assetPaths = Regex.Matches(html, "(?:src|href)=\"(?<path>/assets/[^\"]+)\"")
            .Select(match => match.Groups["path"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(assetPaths);
        foreach (var path in assetPaths)
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Content.Headers.ContentLength > 0);
        }
    }

    [Fact]
    public async Task ProductionHttpsHasHstsAndExactSameOriginCsp()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        Assert.Equal("max-age=31536000", response.Headers.GetValues("Strict-Transport-Security").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("connect-src 'self'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain(" ws:", csp, StringComparison.Ordinal);
        Assert.DoesNotContain(" wss:", csp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthenticatedApiResponsesAreNeverStored()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var response = await factory.CreateClient().GetAsync("/api/sessions");

        response.EnsureSuccessStatusCode();
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-cache", response.Headers.Pragma.Single().Name);
    }

    [Fact]
    public async Task ExternalHttpsProfileRejectsDirectBackendHttpExceptLiveness()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        Assert.Equal(HttpStatusCode.UpgradeRequired, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task StateChangingValidationAndCsrfAreEnforced()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        var token = (await csrf.Content.ReadFromJsonAsync<CsrfResponse>())!.Token;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("TmuxMobile-Csrf-Dev="))
            .Split(';')[0];
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/sessions/{TmuxFactory.Session.Id}/rename")
        {
            Content = JsonContent.Create(new { name = "bad/name" })
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        request.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SessionCreationReturnsOpaqueTargetAndAuditsSuccess()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/sessions",
            new { name = " new-agent " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        Assert.NotNull(created);
        Assert.True(SafeIdentifier.IsSession(created.Id));
        Assert.Equal("new-agent", created.Name);
        Assert.Equal(1, factory.CreateCalls);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "session.create" && record.Target == created.Id && record.Succeeded);
        var sessions = await client.GetFromJsonAsync<TmuxSession[]>("/api/sessions", JsonOptions);
        Assert.Contains(sessions!, session => session.Id == created.Id && session.Name == "new-agent");
    }

    [Theory]
    [InlineData("bad/name", HttpStatusCode.BadRequest)]
    [InlineData("rewritten.name", HttpStatusCode.BadRequest)]
    [InlineData("rewritten:name", HttpStatusCode.BadRequest)]
    [InlineData("work", HttpStatusCode.Conflict)]
    [InlineData("tmux-failure", HttpStatusCode.ServiceUnavailable)]
    public async Task SessionCreationFailuresAreBoundedAndAudited(string name, HttpStatusCode expected)
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/sessions", new { name });

        Assert.Equal(expected, response.StatusCode);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "session.create" && record.Target == "new-session" && !record.Succeeded);
    }

    [Fact]
    public async Task SessionCreationRequiresAuthenticationAndCsrf()
    {
        await using var anonymousFactory = new TmuxFactory(authenticated: false);
        var anonymous = CreateHttpsClient(anonymousFactory);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/sessions", new { name = "new-agent" })).StatusCode);

        await using var authenticatedFactory = new TmuxFactory(authenticated: true);
        var authenticated = CreateHttpsClient(authenticatedFactory);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await authenticated.PostAsJsonAsync("/api/sessions", new { name = "new-agent" })).StatusCode);
        Assert.Equal(0, authenticatedFactory.CreateCalls);
    }

    [Fact]
    public async Task SessionTerminationIsProtectedAuditedAndRefreshesInventory()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await SendWithCsrfAsync(client, HttpMethod.Delete,
            $"/api/sessions/{TmuxFactory.Session.Id}", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, factory.KillCalls);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "session.kill" && record.Target == TmuxFactory.Session.Id && record.Succeeded);
        var sessions = await client.GetFromJsonAsync<TmuxSession[]>("/api/sessions", JsonOptions);
        Assert.Empty(sessions!);
    }

    [Fact]
    public async Task SessionTerminationRejectsUnknownTargetAndTmuxFailure()
    {
        await using var missingFactory = new TmuxFactory(authenticated: true);
        var missingClient = missingFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var missing = await SendWithCsrfAsync(missingClient, HttpMethod.Delete,
            $"/api/sessions/{SafeIdentifier.ForSession("$missing")}", new { });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(0, missingFactory.KillCalls);
        Assert.Contains(missingFactory.AuditRecords,
            record => record.Action == "session.kill" && !record.Succeeded);

        await using var failedFactory = new TmuxFactory(authenticated: true, killFails: true);
        var failedClient = failedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var failed = await SendWithCsrfAsync(failedClient, HttpMethod.Delete,
            $"/api/sessions/{TmuxFactory.Session.Id}", new { });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(0, failedFactory.KillCalls);
        Assert.Contains(failedFactory.AuditRecords,
            record => record.Action == "session.kill" && !record.Succeeded);
    }

    [Fact]
    public async Task SessionTerminationRequiresAuthenticationAndCsrf()
    {
        await using var anonymousFactory = new TmuxFactory(authenticated: false);
        var anonymous = CreateHttpsClient(anonymousFactory);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.DeleteAsync($"/api/sessions/{TmuxFactory.Session.Id}")).StatusCode);

        await using var authenticatedFactory = new TmuxFactory(authenticated: true);
        var authenticated = CreateHttpsClient(authenticatedFactory);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await authenticated.DeleteAsync($"/api/sessions/{TmuxFactory.Session.Id}")).StatusCode);
        Assert.Equal(0, authenticatedFactory.KillCalls);
    }

    [Fact]
    public async Task SessionTerminationUsesInteractionRateLimit()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        var token = (await csrf.Content.ReadFromJsonAsync<CsrfResponse>())!.Token;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("TmuxMobile-Csrf-Dev="))
            .Split(';')[0];
        var missingId = SafeIdentifier.ForSession("$missing");

        for (var attempt = 0; attempt < 60; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/sessions/{missingId}");
            request.Headers.Add("X-CSRF-TOKEN", token);
            request.Headers.Add("Cookie", cookie);
            Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(request)).StatusCode);
        }
        using var limitedRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/sessions/{missingId}");
        limitedRequest.Headers.Add("X-CSRF-TOKEN", token);
        limitedRequest.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(limitedRequest)).StatusCode);
        Assert.Equal(0, factory.KillCalls);
    }

    [Fact]
    public async Task AuditSinkFailureDoesNotMisreportAnAppliedAction()
    {
        await using var factory = new TmuxFactory(authenticated: true, auditSucceeds: false);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var response = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/sessions/{TmuxFactory.Session.Id}/rename", new { name = "renamed-work" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, factory.RenameCalls);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "session.rename" && record.Succeeded);
    }

    [Fact]
    public async Task FailedAndTextInteractionsAreAuditedWithoutTerminalContents()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost")
        });
        var invalid = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/sessions/{TmuxFactory.Session.Id}/rename", new { name = "bad/name" });
        const string terminalText = "do-not-record-this-terminal-input";
        var sent = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/panes/{TmuxFactory.Session.CurrentPaneId}/text", new { text = terminalText });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "session.rename" && !record.Succeeded);
        Assert.Contains(factory.AuditRecords,
            record => record.Action == "pane.text" && record.Succeeded);
        Assert.DoesNotContain(factory.AuditRecords,
            record => record.Action.Contains(terminalText, StringComparison.Ordinal) ||
                      record.Subject.Contains(terminalText, StringComparison.Ordinal) ||
                      record.Target.Contains(terminalText, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OversizedRequestBodyIsRejectedBeforeBinding()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var content = new StringContent(new string('x', 70_000), Encoding.UTF8, "application/json");
        var response = await factory.CreateClient().PostAsync("/api/auth/login", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task WebSocketRequiresAuthentication()
    {
        await using var factory = new TmuxFactory(authenticated: false);
        var client = factory.Server.CreateWebSocketClient();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.ConnectAsync(new Uri("ws://localhost/ws/inventory"), CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizedTerminalWebSocketStreamsPtyAndCleansUp()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.Server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(
            new Uri($"ws://localhost/ws/terminal/{TmuxFactory.Session.Id}"), CancellationToken.None);
        var buffer = new byte[64];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        Assert.Equal("pty-ready", Encoding.UTF8.GetString(buffer, 0, result.Count));
    }

    [Fact]
    public async Task TerminalHistoryMessageUsesTmuxControlWithoutWritingPtyInput()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.Server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(
            new Uri($"ws://localhost/ws/terminal/{TmuxFactory.Session.Id}"), CancellationToken.None);
        var buffer = new byte[64];
        await socket.ReceiveAsync(buffer, CancellationToken.None);

        await socket.SendAsync(
            Encoding.UTF8.GetBytes("""{"type":"history","action":"older","pages":999}"""),
            WebSocketMessageType.Text, true, CancellationToken.None);
        await WaitUntilAsync(() => factory.HistoryCalls.Count >= 1);

        var call = factory.HistoryCalls[0];
        Assert.Equal((TerminalHistoryAction.Older, 3), (call.Action, call.Pages));
        Assert.Equal(0, factory.LastPtyInputLength);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test complete", CancellationToken.None);
        await WaitUntilAsync(() => factory.HistoryCalls.Count >= 2);
        Assert.Equal(TerminalHistoryAction.Latest, factory.HistoryCalls[^1].Action);
    }

    [Fact]
    public async Task TerminalHistoryMessageRejectsUnknownAction()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.Server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(
            new Uri($"ws://localhost/ws/terminal/{TmuxFactory.Session.Id}"), CancellationToken.None);
        var buffer = new byte[64];
        await socket.ReceiveAsync(buffer, CancellationToken.None);
        await socket.SendAsync(
            Encoding.UTF8.GetBytes("""{"type":"history","action":"run-shell","pages":1}"""),
            WebSocketMessageType.Text, true, CancellationToken.None);

        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.InvalidPayloadData, socket.CloseStatus);
        Assert.Empty(factory.HistoryCalls);
        Assert.Equal(0, factory.LastPtyInputLength);
    }

    [Fact]
    public async Task TerminalHistoryMessageRateLimitRejectsProcessBurst()
    {
        await using var factory = new TmuxFactory(authenticated: true);
        var client = factory.Server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(
            new Uri($"ws://localhost/ws/terminal/{TmuxFactory.Session.Id}"), CancellationToken.None);
        var buffer = new byte[64];
        await socket.ReceiveAsync(buffer, CancellationToken.None);
        var message = Encoding.UTF8.GetBytes(
            """{"type":"history","action":"older","pages":1}""");
        for (var index = 0; index < 5; index++)
            await socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);

        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, socket.CloseStatus);
        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ack", CancellationToken.None);
        await WaitUntilAsync(() => factory.HistoryCalls.Count >= 5);
        Assert.Equal(4, factory.HistoryCalls.Count(call => call.Action == TerminalHistoryAction.Older));
        Assert.Equal(TerminalHistoryAction.Latest, factory.HistoryCalls[^1].Action);
        Assert.Equal(0, factory.LastPtyInputLength);
    }

    [Fact]
    public async Task TerminalInputMessageRateLimitRejectsBurst()
    {
        await using var factory = new TmuxFactory(authenticated: true, new Dictionary<string, string?>
        {
            ["Security:MaxTerminalInputMessagesPerSecond"] = "4",
            ["Security:MaxTerminalInputBytesPerSecond"] = "1024"
        });
        var client = factory.Server.CreateWebSocketClient();
        using var socket = await client.ConnectAsync(
            new Uri($"ws://localhost/ws/terminal/{TmuxFactory.Session.Id}"), CancellationToken.None);
        var buffer = new byte[64];
        await socket.ReceiveAsync(buffer, CancellationToken.None);
        for (var index = 0; index < 5; index++)
            await socket.SendAsync("x"u8.ToArray(), WebSocketMessageType.Binary, true, CancellationToken.None);

        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, socket.CloseStatus);
        Assert.Equal(4, factory.LastPtyInputLength);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
            await Task.Delay(10);
        Assert.True(condition(), "Timed out waiting for terminal history operation.");
    }

    private static HttpClient CreateHttpsClient(TmuxFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(HttpClient client,
        HttpMethod method, string path, object body)
    {
        var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        var token = (await csrf.Content.ReadFromJsonAsync<CsrfResponse>())!.Token;
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("TmuxMobile-Csrf-Dev="))
            .Split(';')[0];
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private sealed record CsrfResponse(string Token);
}

public sealed class TmuxFactory(bool authenticated,
    IReadOnlyDictionary<string, string?>? overrides = null,
    bool auditSucceeds = true,
    bool killFails = false,
    bool workspaceRecovery = false,
    bool emptySessions = false) : WebApplicationFactory<Program>
{
    private readonly FakeTmuxService fakeTmux = new(killFails, emptySessions);
    private readonly FakePseudoTerminalFactory fakePty = new();
    private readonly FakeAuditLogger fakeAudit = new(auditSucceeds);
    private readonly string auditPath = Path.Combine(Path.GetTempPath(),
        $"tmux-mobile-tests-{Guid.NewGuid():N}", "audit.jsonl");
    private readonly string recoveryPath = Path.Combine(Path.GetTempPath(),
        $"tmux-mobile-recovery-tests-{Guid.NewGuid():N}");

    public static readonly TmuxSession Session = new(
        SafeIdentifier.ForSession("$test"), "work", DateTimeOffset.UnixEpoch,
        DateTimeOffset.UtcNow, false, 0, 1, 1, "main", SafeIdentifier.ForPane("%test"),
        "dotnet", "/srv/work", "build", SessionStatus.Active, "Running", "preview");

    public IReadOnlyList<HistoryCall> HistoryCalls => fakeTmux.HistoryCalls.ToArray();
    public IReadOnlyList<AuditRecord> AuditRecords => fakeAudit.Records.ToArray();
    public long LastPtyInputLength => fakePty.Last?.InputLength ?? 0;
    public int RenameCalls => fakeTmux.RenameCalls;
    public int CreateCalls => fakeTmux.CreateCalls;
    public int KillCalls => fakeTmux.KillCalls;
    public string RestoreRequestPath => Path.Combine(recoveryPath, "restore.request");

    public void WriteRecoverySnapshot()
    {
        Directory.CreateDirectory(recoveryPath);
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(recoveryPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(recoveryPath, "workspace.v1.tsv");
        File.WriteAllText(path, "tmux-mobile-workspace\t1\n");
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    public sealed record HistoryCall(TerminalHistoryAction Action, int Pages);
    public sealed record AuditRecord(string Action, string Subject, string Target, bool Succeeded);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(recoveryPath);
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(recoveryPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        builder.UseEnvironment(authenticated ? "Development" : "Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = authenticated ? "Development" : "ApiKey",
                ["Authentication:AllowDevelopmentBypass"] = authenticated ? "true" : "false",
                ["Authentication:ApiKey"] = "test-secret-at-least-24-characters",
                ["Security:AllowedOrigins:0"] = authenticated ? "http://localhost" : "https://localhost",
                ["Security:ExternalHttpsTermination"] = authenticated ? "false" : "true",
                ["Audit:Destination"] = auditPath,
                ["WorkspaceRecovery:Enabled"] = workspaceRecovery ? "true" : "false",
                ["WorkspaceRecovery:ControlDirectory"] = recoveryPath
            };
            if (overrides is not null)
                foreach (var pair in overrides) values[pair.Key] = pair.Value;
            configuration.AddInMemoryCollection(values);
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITmuxService>();
            services.RemoveAll<ITmuxTargetResolver>();
            services.RemoveAll<IPseudoTerminalFactory>();
            services.RemoveAll<IAuditLogger>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton<ITmuxService>(fakeTmux);
            services.AddSingleton<ITmuxTargetResolver, FakeTargetResolver>();
            services.AddSingleton<IPseudoTerminalFactory>(fakePty);
            services.AddSingleton<IAuditLogger>(fakeAudit);
        });
    }

    private sealed class FakeAuditLogger(bool succeeds) : IAuditLogger
    {
        public ConcurrentQueue<AuditRecord> Records { get; } = new();

        public Task<bool> WriteAsync(string action, string subject, string target, bool succeeded,
            CancellationToken cancellationToken)
        {
            Records.Enqueue(new(action, subject, target, succeeded));
            return Task.FromResult(succeeds);
        }
    }

    private sealed class FakeTargetResolver : ITmuxTargetResolver
    {
        public Task<string?> ResolveRawSessionAsync(string safeSessionId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(safeSessionId == Session.Id ? "$test" : null);
    }

    private sealed class FakePseudoTerminalFactory : IPseudoTerminalFactory
    {
        public FakePseudoTerminal? Last { get; private set; }

        public Task<IPseudoTerminal> StartAsync(string executable, IReadOnlyList<string> arguments,
            TerminalSize size, IReadOnlyDictionary<string, string> environment,
            CancellationToken cancellationToken)
        {
            Last = new FakePseudoTerminal();
            return Task.FromResult<IPseudoTerminal>(Last);
        }
    }

    private sealed class FakePseudoTerminal : IPseudoTerminal
    {
        public Stream Input { get; } = new MemoryStream();
        public Stream Output { get; } = new BlockingOutputStream("pty-ready"u8.ToArray());
        public long InputLength => ((MemoryStream)Input).ToArray().LongLength;
        public int ProcessId => 4242;
        public bool HasExited { get; private set; }
        public ValueTask ResizeAsync(TerminalSize size, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            Task.Delay(Timeout.Infinite, cancellationToken);
        public ValueTask DisposeAsync()
        {
            HasExited = true;
            Input.Dispose();
            Output.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingOutputStream(byte[] initial) : Stream
    {
        private int offset;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => initial.Length;
        public override long Position { get => offset; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int bufferOffset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (offset < initial.Length)
            {
                var count = Math.Min(buffer.Length, initial.Length - offset);
                initial.AsMemory(offset, count).CopyTo(buffer);
                offset += count;
                return count;
            }
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class FakeTmuxService(bool killFails, bool emptySessions) : ITmuxService
    {
        private readonly List<TmuxSession> sessions = emptySessions ? [] : [Session];
        public ConcurrentQueue<HistoryCall> HistoryCalls { get; } = new();
        public int RenameCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public int KillCalls { get; private set; }
        public Task<IReadOnlyList<TmuxSession>> GetSessionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TmuxSession>>(sessions.ToArray());
        public Task<CreatedTmuxSession> CreateSessionAsync(string name, CancellationToken cancellationToken)
        {
            var normalized = InputValidation.ValidateCreateName(name);
            if (normalized == "work") throw new TmuxConflictException("A session with that name already exists.");
            if (normalized == "tmux-failure") throw new TmuxCommandException("internal tmux detail");
            CreateCalls++;
            var rawId = $"${CreateCalls + 100}";
            var created = new CreatedTmuxSession(SafeIdentifier.ForSession(rawId), normalized);
            sessions.Add(Session with
            {
                Id = created.Id,
                Name = created.Name,
                CurrentPaneId = SafeIdentifier.ForPane($"%{CreateCalls + 100}")
            });
            return Task.FromResult(created);
        }
        public Task<TmuxSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken) =>
            Task.FromResult<TmuxSession?>(sessions.SingleOrDefault(session => session.Id == sessionId));
        public Task<IReadOnlyList<TmuxPane>> GetPanesAsync(string sessionId, CancellationToken cancellationToken)
        {
            if (sessionId != Session.Id) throw new TmuxNotFoundException("Missing");
            return Task.FromResult<IReadOnlyList<TmuxPane>>([
                new(Session.CurrentPaneId, Session.Id, 0, 0, "pane", "dotnet", "/srv/work", true, 123, 80, 24)
            ]);
        }
        public Task<string> CapturePaneAsync(string paneId, int historyLines, CancellationToken cancellationToken)
        {
            if (paneId != Session.CurrentPaneId) throw new TmuxNotFoundException("Missing");
            return Task.FromResult("safe <text>");
        }
        public Task RenameSessionAsync(string sessionId, string newName, CancellationToken cancellationToken)
        {
            InputValidation.ValidateRename(newName);
            if (sessionId != Session.Id) throw new TmuxNotFoundException("Missing");
            RenameCalls++;
            return Task.CompletedTask;
        }
        public Task KillSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            var session = sessions.SingleOrDefault(candidate => candidate.Id == sessionId)
                ?? throw new TmuxNotFoundException("Missing");
            if (killFails) throw new TmuxCommandException("internal tmux detail");
            sessions.Remove(session);
            KillCalls++;
            return Task.CompletedTask;
        }
        public Task SendKeysAsync(string paneId, IReadOnlyList<TmuxKey> keys, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task SendTextAsync(string paneId, string text, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task InterruptPaneAsync(string paneId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> ControlSessionHistoryAsync(string sessionId, TerminalHistoryAction action, int pages,
            CancellationToken cancellationToken)
        {
            if (sessionId != Session.Id) throw new TmuxNotFoundException("Missing");
            HistoryCalls.Enqueue(new(action, pages));
            return Task.FromResult(action == TerminalHistoryAction.Older);
        }
    }
}
