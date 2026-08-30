using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;
using TmuxMobile.Infrastructure;
using TmuxMobile.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TmuxOptions>().BindConfiguration(TmuxOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<SecurityOptions>().BindConfiguration(SecurityOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<StatusOptions>().BindConfiguration(StatusOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AuditOptions>().BindConfiguration(AuditOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DataProtectionSettings>().BindConfiguration(DataProtectionSettings.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ForwardedHeaderSettings>().BindConfiguration(ForwardedHeaderSettings.Section)
    .ValidateOnStart();
builder.Services.AddOptions<WorkspaceRecoveryOptions>().BindConfiguration(WorkspaceRecoveryOptions.Section)
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<SecurityConfigurationValidator>();
builder.Services.AddSingleton<IValidateOptions<AuthOptions>>(sp => sp.GetRequiredService<SecurityConfigurationValidator>());
builder.Services.AddSingleton<IValidateOptions<SecurityOptions>>(sp => sp.GetRequiredService<SecurityConfigurationValidator>());
builder.Services.AddSingleton<IValidateOptions<TmuxOptions>>(sp => sp.GetRequiredService<SecurityConfigurationValidator>());
builder.Services.AddSingleton<IValidateOptions<ForwardedHeaderSettings>>(sp =>
    sp.GetRequiredService<SecurityConfigurationValidator>());
builder.Services.AddSingleton<IValidateOptions<WorkspaceRecoveryOptions>>(sp =>
    sp.GetRequiredService<SecurityConfigurationValidator>());

var auth = builder.Configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new();
var dataProtection = builder.Configuration.GetSection(DataProtectionSettings.Section)
    .Get<DataProtectionSettings>() ?? new();
var keyDirectory = Path.IsPathFullyQualified(dataProtection.KeysDirectory)
    ? dataProtection.KeysDirectory
    : Path.Combine(builder.Environment.ContentRootPath, dataProtection.KeysDirectory);
Directory.CreateDirectory(keyDirectory);
builder.Services.AddDataProtection()
    .SetApplicationName("TmuxMobile")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
var useDevelopmentAuth = auth.Mode.Equals("Development", StringComparison.OrdinalIgnoreCase);
var useInsecureHttpCookies = auth.UnsafeAllowInsecureHttp;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = useDevelopmentAuth ? "Development" : CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = useDevelopmentAuth ? "Development" : CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = useInsecureHttpCookies ? "TmuxMobile-InsecureTest" : "__Host-TmuxMobile";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = useInsecureHttpCookies
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
})
.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Read", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "read"));
    options.AddPolicy("Interact", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "interact"));
    options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireClaim("permission", "admin"));
    options.AddPolicy("Readiness", policy => policy.RequireAssertion(context =>
        context.User.HasClaim("permission", "read") ||
        context.Resource is HttpContext http && IsLoopback(http.Connection.RemoteIpAddress)));
    options.FallbackPolicy = options.GetPolicy("Read");
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = useInsecureHttpCookies
        ? "TmuxMobile-Csrf-InsecureTest"
        : builder.Environment.IsDevelopment()
            ? "TmuxMobile-Csrf-Dev" : "__Host-TmuxMobile-Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = useInsecureHttpCookies || builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context, includeIdentity: true),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context, includeIdentity: false), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    options.AddPolicy("interact", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context, includeIdentity: true), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    options.AddPolicy("health", context => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(context, includeIdentity: false), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = false;
    options.Preload = false;
    options.ExcludedHosts.Clear();
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<TmuxReadinessHealthCheck>("tmux", tags: ["ready"]);
var forwarded = builder.Configuration.GetSection(ForwardedHeaderSettings.Section)
    .Get<ForwardedHeaderSettings>() ?? new();
if (forwarded.Enabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in forwarded.KnownProxies)
        {
            if (!IPAddress.TryParse(proxy, out var address))
                throw new InvalidOperationException($"ForwardedHeaders known proxy '{proxy}' is not an IP address.");
            options.KnownProxies.Add(address);
        }
        foreach (var host in forwarded.KnownProxyHosts)
        {
            IPAddress[] addresses;
            try
            {
                addresses = Dns.GetHostAddresses(host);
            }
            catch (Exception exception) when (exception is System.Net.Sockets.SocketException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders known proxy host '{host}' could not be resolved.", exception);
            }
            if (addresses.Length == 0)
                throw new InvalidOperationException(
                    $"ForwardedHeaders known proxy host '{host}' resolved to no addresses.");
            foreach (var address in addresses)
                options.KnownProxies.Add(address);
        }
    });
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<ISessionAnalyzer>(sp =>
    new RuleBasedSessionAnalyzer(sp.GetRequiredService<IOptions<StatusOptions>>().Value));
builder.Services.AddSingleton<TmuxService>();
builder.Services.AddSingleton<ITmuxService>(sp => sp.GetRequiredService<TmuxService>());
builder.Services.AddSingleton<ITmuxTargetResolver>(sp => sp.GetRequiredService<TmuxService>());
builder.Services.AddSingleton<IPseudoTerminalFactory, LinuxPseudoTerminalFactory>();
builder.Services.AddSingleton<InventoryStore>();
builder.Services.AddSingleton<IInventoryStore>(sp => sp.GetRequiredService<InventoryStore>());
builder.Services.AddHostedService<InventoryPollingService>();
builder.Services.AddSingleton<IAuditLogger, JsonLineAuditLogger>();
builder.Services.AddHostedService<AuditStorageStartupService>();
builder.Services.AddSingleton<WorkspaceRecoveryControl>();
builder.Services.AddHostedService<WorkspaceRecoveryStartupService>();
builder.Services.AddSingleton<TerminalConnectionLimiter>();
builder.Services.AddSingleton<WebSocketHandlers>();

var security = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>() ?? new();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = security.MaxRequestBodyBytes);

var app = builder.Build();
security = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;
app.Logger.LogInformation("Tmux Mobile starting in {Environment}", app.Environment.EnvironmentName);
if (useInsecureHttpCookies)
    app.Logger.LogWarning(
        "UNSAFE TEST MODE: authentication cookies may be sent over HTTP. Bind only to a trusted tailnet address.");
if (auth.UnsafeAllowWeakApiKeyForTest)
    app.Logger.LogWarning(
        "UNSAFE TEST MODE: the minimum API key length is reduced to eight characters for this test instance.");
if (forwarded.Enabled) app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var badRequest = exception is AntiforgeryValidationException or BadHttpRequestException or System.Text.Json.JsonException;
    var statusCode = badRequest ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = statusCode;
    await Results.Problem(badRequest
        ? "The request is malformed or failed security validation."
        : "The server could not complete the request.", statusCode: statusCode).ExecuteAsync(context);
}));
app.Use(async (context, next) =>
{
    var isLocalReadiness = context.Request.Path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase) &&
                           IsLoopback(context.Connection.RemoteIpAddress);
    if (security.ExternalHttpsTermination && !context.Request.IsHttps &&
        !context.Request.Path.Equals("/health/live", StringComparison.OrdinalIgnoreCase) &&
        !isLocalReadiness)
    {
        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        context.Response.Headers.Upgrade = "TLS/1.2, HTTP/1.1";
        await Results.Problem("This backend accepts application traffic only through the configured HTTPS terminator.",
            statusCode: StatusCodes.Status426UpgradeRequired).ExecuteAsync(context);
        return;
    }
    if (context.Request.ContentLength > security.MaxRequestBodyBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        return;
    }
    await next();
});
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; img-src 'self' data:; font-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next();
});
var webSocketOptions = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) };
foreach (var origin in security.AllowedOrigins) webSocketOptions.AllowedOrigins.Add(origin);
app.UseWebSockets(webSocketOptions);
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name is "service-worker.js" or "index.html")
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (
    LoginRequest request, HttpContext context, IOptions<AuthOptions> options, IAuditLogger auditLogger) =>
{
    var configured = options.Value.ApiKey ?? "";
    var supplied = request.ApiKey ?? "";
    var valid = configured.Length == supplied.Length &&
                CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configured),
                    Encoding.UTF8.GetBytes(supplied));
    if (!valid)
    {
        await auditLogger.WriteAsync("auth.login", "anonymous", "local", false, context.RequestAborted);
        return Results.Unauthorized();
    }
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "owner"),
        new Claim(ClaimTypes.Name, "Owner"),
        new Claim("permission", "read"), new Claim("permission", "interact"), new Claim("permission", "admin")
    };
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = true });
    await auditLogger.WriteAsync("auth.login", "owner", "local", true, context.RequestAborted);
    return Results.NoContent();
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/api/auth/logout", async (HttpContext context, IAntiforgery antiforgery,
    IAuditLogger auditLogger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var user = UserId(context.User);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await auditLogger.WriteAsync("auth.logout", user, "local", true, context.RequestAborted);
    return Results.NoContent();
}).RequireAuthorization("Read");

app.MapGet("/api/auth/status", (ClaimsPrincipal user) => Results.Ok(new
{
    authenticated = user.Identity?.IsAuthenticated == true,
    name = user.Identity?.Name
})).RequireAuthorization("Read");
app.MapGet("/api/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }))
    .RequireAuthorization("Read");
app.MapGet("/api/config", (IOptions<TmuxOptions> options) => Results.Ok(new
{
    tmuxPrefix = options.Value.Prefix
})).RequireAuthorization("Read");
app.MapGet("/api/desktop/capabilities", () => Results.Ok(new DesktopCapabilities(
        DesktopProtocol.CurrentVersion,
        DesktopProtocol.MinimumSupportedClientVersion,
        DesktopProtocol.RequiredFeatures)))
    .AllowAnonymous()
    .RequireRateLimiting("health");

var workspaceRecovery = app.MapGroup("/api/workspace-recovery").RequireAuthorization("Read");
workspaceRecovery.MapGet("/", (WorkspaceRecoveryControl control) => Results.Ok(control.GetStatus()));
workspaceRecovery.MapPost("/restore", async (
    HttpContext context, WorkspaceRecoveryControl control, ITmuxService tmux,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
{
    const string auditTarget = "saved-workspace";
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
            context.RequestAborted);
        return Results.BadRequest(new { error = "The CSRF token is missing or invalid." });
    }
    try
    {
        if ((await tmux.GetSessionsAsync(context.RequestAborted)).Count != 0)
        {
            await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
                context.RequestAborted);
            return Results.Conflict(new { error = "Restore is available only when no tmux sessions are running." });
        }
        var requestId = await control.RequestRestoreAsync(context.RequestAborted);
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), requestId.ToString("D"), true,
            context.RequestAborted);
        return Results.Accepted("/api/workspace-recovery", new { requestId });
    }
    catch (WorkspaceRecoveryDisabledException)
    {
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
            context.RequestAborted);
        return Results.Problem("Workspace recovery is not enabled.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (WorkspaceSnapshotUnavailableException)
    {
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
            context.RequestAborted);
        return Results.Conflict(new { error = "No saved workspace is available." });
    }
    catch (WorkspaceRestorePendingException)
    {
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
            context.RequestAborted);
        return Results.Conflict(new { error = "A restore request is already pending." });
    }
    catch
    {
        await auditLogger.WriteAsync("workspace.restore.request", UserId(context.User), auditTarget, false,
            CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Admin").RequireRateLimiting("interact");

var sessions = app.MapGroup("/api/sessions").RequireAuthorization("Read");
sessions.MapGet("/", async (ITmuxService tmux, CancellationToken cancellationToken) =>
    Results.Ok(await tmux.GetSessionsAsync(cancellationToken)));
sessions.MapPost("/", async (
    CreateSessionRequest request, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        await auditLogger.WriteAsync("session.create", UserId(context.User), "new-session", false,
            context.RequestAborted);
        return Results.BadRequest(new { error = "The CSRF token is missing or invalid." });
    }
    try
    {
        var created = await tmux.CreateSessionAsync(request.Name, context.RequestAborted);
        await inventory.RefreshAsync(context.RequestAborted);
        await auditLogger.WriteAsync("session.create", UserId(context.User), created.Id, true,
            context.RequestAborted);
        return Results.Created($"/api/sessions/{created.Id}", new CreateSessionResponse(created.Id, created.Name));
    }
    catch (ArgumentException exception)
    {
        await auditLogger.WriteAsync("session.create", UserId(context.User), "new-session", false,
            context.RequestAborted);
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (TmuxConflictException exception)
    {
        await auditLogger.WriteAsync("session.create", UserId(context.User), "new-session", false,
            context.RequestAborted);
        return Results.Conflict(new { error = exception.Message });
    }
    catch (TmuxCommandException)
    {
        await auditLogger.WriteAsync("session.create", UserId(context.User), "new-session", false,
            CancellationToken.None);
        return Results.Problem("tmux could not create the session.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        await auditLogger.WriteAsync("session.create", UserId(context.User), "new-session", false,
            CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Interact").RequireRateLimiting("interact");
sessions.MapGet("/{sessionId}", async (string sessionId, ITmuxService tmux, CancellationToken cancellationToken) =>
    await tmux.GetSessionAsync(sessionId, cancellationToken) is { } session
        ? Results.Ok(session) : Results.NotFound());
sessions.MapGet("/{sessionId}/panes", async (string sessionId, ITmuxService tmux, CancellationToken cancellationToken) =>
{
    try { return Results.Ok(await tmux.GetPanesAsync(sessionId, cancellationToken)); }
    catch (TmuxNotFoundException) { return Results.NotFound(); }
});
sessions.MapGet("/{sessionId}/topology", async (
    string sessionId, ITmuxService tmux, CancellationToken cancellationToken) =>
{
    try { return Results.Ok(await tmux.GetTopologyAsync(sessionId, cancellationToken)); }
    catch (TmuxNotFoundException) { return Results.NotFound(); }
});
sessions.MapPost("/{sessionId}/windows", async (
    string sessionId, CreateWindowRequest request, HttpContext context, ITmuxService tmux,
    IInventoryStore inventory, IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "window.create", sessionId, async cancellationToken =>
            await tmux.CreateWindowAsync(sessionId, request.Name, cancellationToken)))
    .RequireAuthorization("Interact").RequireRateLimiting("interact");
sessions.MapDelete("/{sessionId}", async (
    string sessionId, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger, ILogger<Program> logger) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        await auditLogger.WriteAsync("session.kill", UserId(context.User), sessionId, false,
            context.RequestAborted);
        return Results.BadRequest(new { error = "The CSRF token is missing or invalid." });
    }
    try
    {
        await tmux.KillSessionAsync(sessionId, context.RequestAborted);
        try
        {
            await inventory.RefreshAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Session {SessionId} was terminated but the immediate inventory refresh failed", sessionId);
        }
        await auditLogger.WriteAsync("session.kill", UserId(context.User), sessionId, true,
            CancellationToken.None);
        return Results.NoContent();
    }
    catch (TmuxNotFoundException)
    {
        await auditLogger.WriteAsync("session.kill", UserId(context.User), sessionId, false,
            context.RequestAborted);
        return Results.NotFound();
    }
    catch (TmuxCommandException)
    {
        await auditLogger.WriteAsync("session.kill", UserId(context.User), sessionId, false,
            CancellationToken.None);
        return Results.Problem("tmux could not terminate the session.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        await auditLogger.WriteAsync("session.kill", UserId(context.User), sessionId, false,
            CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Admin").RequireRateLimiting("interact");
sessions.MapPost("/{sessionId}/rename", async (
    string sessionId, RenameRequest request, HttpContext context, ITmuxService tmux,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    try
    {
        await tmux.RenameSessionAsync(sessionId, request.Name, context.RequestAborted);
        await auditLogger.WriteAsync("session.rename", UserId(context.User), sessionId, true, context.RequestAborted);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        await auditLogger.WriteAsync("session.rename", UserId(context.User), sessionId, false,
            context.RequestAborted);
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (TmuxNotFoundException)
    {
        await auditLogger.WriteAsync("session.rename", UserId(context.User), sessionId, false,
            context.RequestAborted);
        return Results.NotFound();
    }
    catch
    {
        await auditLogger.WriteAsync("session.rename", UserId(context.User), sessionId, false,
            CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Interact").RequireRateLimiting("interact");

var windows = app.MapGroup("/api/windows").RequireAuthorization("Read");
windows.MapPost("/{windowId}/select", async (
    string windowId, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "window.select", windowId, async cancellationToken =>
        {
            await tmux.SelectWindowAsync(windowId, cancellationToken);
            return null;
        })).RequireAuthorization("Interact").RequireRateLimiting("interact");
windows.MapDelete("/{windowId}", async (
    string windowId, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "window.kill", windowId, async cancellationToken =>
        {
            await tmux.KillWindowAsync(windowId, cancellationToken);
            return null;
        })).RequireAuthorization("Interact").RequireRateLimiting("interact");

var panes = app.MapGroup("/api/panes").RequireAuthorization("Read");
panes.MapGet("/{paneId}/capture", async (
    string paneId, int? lines, ITmuxService tmux, IOptions<TmuxOptions> options,
    CancellationToken cancellationToken) =>
{
    var count = Math.Clamp(lines ?? 200, 1, options.Value.MaxCaptureLines);
    try { return Results.Ok(new CaptureResponse(await tmux.CapturePaneAsync(paneId, count, cancellationToken), count)); }
    catch (TmuxNotFoundException) { return Results.NotFound(); }
});
panes.MapPost("/{paneId}/split", async (
    string paneId, SplitPaneRequest request, HttpContext context, ITmuxService tmux,
    IInventoryStore inventory, IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "pane.split", paneId, async cancellationToken =>
            await tmux.SplitPaneAsync(paneId, request.Orientation, cancellationToken)))
    .RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapPost("/{paneId}/select", async (
    string paneId, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "pane.select", paneId, async cancellationToken =>
        {
            await tmux.SelectPaneAsync(paneId, cancellationToken);
            return null;
        })).RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapPost("/{paneId}/resize", async (
    string paneId, ResizePaneRequest request, HttpContext context, ITmuxService tmux,
    IInventoryStore inventory, IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "pane.resize", paneId, async cancellationToken =>
        {
            await tmux.ResizePaneAsync(paneId, request.Direction, request.Cells, cancellationToken);
            return null;
        })).RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapDelete("/{paneId}", async (
    string paneId, HttpContext context, ITmuxService tmux, IInventoryStore inventory,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
    await ExecuteTopologyActionAsync(context, antiforgery, auditLogger, inventory,
        "pane.kill", paneId, async cancellationToken =>
        {
            await tmux.KillPaneAsync(paneId, cancellationToken);
            return null;
        })).RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapPost("/{paneId}/keys", async (
    string paneId, KeysRequest request, HttpContext context, ITmuxService tmux,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    try
    {
        await tmux.SendKeysAsync(paneId, request.Keys, context.RequestAborted);
        await auditLogger.WriteAsync("pane.keys", UserId(context.User), paneId, true, context.RequestAborted);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        await auditLogger.WriteAsync("pane.keys", UserId(context.User), paneId, false, context.RequestAborted);
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (TmuxNotFoundException)
    {
        await auditLogger.WriteAsync("pane.keys", UserId(context.User), paneId, false, context.RequestAborted);
        return Results.NotFound();
    }
    catch
    {
        await auditLogger.WriteAsync("pane.keys", UserId(context.User), paneId, false, CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapPost("/{paneId}/text", async (
    string paneId, TextRequest request, HttpContext context, ITmuxService tmux,
    IAntiforgery antiforgery, IAuditLogger auditLogger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    try
    {
        await tmux.SendTextAsync(paneId, request.Text, context.RequestAborted);
        await auditLogger.WriteAsync("pane.text", UserId(context.User), paneId, true, context.RequestAborted);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        await auditLogger.WriteAsync("pane.text", UserId(context.User), paneId, false, context.RequestAborted);
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (TmuxNotFoundException)
    {
        await auditLogger.WriteAsync("pane.text", UserId(context.User), paneId, false, context.RequestAborted);
        return Results.NotFound();
    }
    catch
    {
        await auditLogger.WriteAsync("pane.text", UserId(context.User), paneId, false, CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Interact").RequireRateLimiting("interact");
panes.MapPost("/{paneId}/interrupt", async (
    string paneId, HttpContext context, ITmuxService tmux, IAntiforgery antiforgery,
    IAuditLogger auditLogger) =>
{
    await antiforgery.ValidateRequestAsync(context);
    try
    {
        await tmux.InterruptPaneAsync(paneId, context.RequestAborted);
        await auditLogger.WriteAsync("pane.interrupt", UserId(context.User), paneId, true, context.RequestAborted);
        return Results.NoContent();
    }
    catch (TmuxNotFoundException)
    {
        await auditLogger.WriteAsync("pane.interrupt", UserId(context.User), paneId, false,
            context.RequestAborted);
        return Results.NotFound();
    }
    catch
    {
        await auditLogger.WriteAsync("pane.interrupt", UserId(context.User), paneId, false,
            CancellationToken.None);
        throw;
    }
}).RequireAuthorization("Interact").RequireRateLimiting("interact");

app.Map("/ws/inventory", async (HttpContext context, WebSocketHandlers handlers) =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    await handlers.InventoryAsync(context);
}).RequireAuthorization("Read");
app.Map("/ws/terminal/{sessionId}", async (HttpContext context, string sessionId, WebSocketHandlers handlers) =>
    await handlers.TerminalAsync(context, sessionId)).RequireAuthorization("Interact");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
}).AllowAnonymous().RequireRateLimiting("health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).RequireAuthorization("Readiness").RequireRateLimiting("health");
app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapFallbackToFile("/desktop/{*path:nonfile}", "desktop/index.html").AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Lifetime.ApplicationStopped.Register(() => app.Logger.LogInformation("Tmux Mobile stopped"));
app.Run();

static string UserId(ClaimsPrincipal user) =>
    user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

static string PartitionKey(HttpContext context, bool includeIdentity)
{
    var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
    var identity = includeIdentity
        ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"
        : "anonymous";
    return $"{identity}|{address}";
}

static bool IsLoopback(IPAddress? address) => address is not null && IPAddress.IsLoopback(address);

static async Task<IResult> ExecuteTopologyActionAsync(
    HttpContext context, IAntiforgery antiforgery, IAuditLogger audit, IInventoryStore inventory,
    string action, string target, Func<CancellationToken, Task<object?>> operation)
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException)
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, context.RequestAborted);
        return Results.BadRequest(new { error = "The CSRF token is missing or invalid." });
    }
    try
    {
        var result = await operation(context.RequestAborted);
        await inventory.RefreshAsync(context.RequestAborted);
        await audit.WriteAsync(action, UserId(context.User), target, true, context.RequestAborted);
        return result is null ? Results.NoContent() : Results.Ok(result);
    }
    catch (ArgumentException exception)
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, context.RequestAborted);
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (TmuxNotFoundException)
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, context.RequestAborted);
        return Results.NotFound();
    }
    catch (TmuxConflictException exception)
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, context.RequestAborted);
        return Results.Conflict(new { error = exception.Message });
    }
    catch (TmuxCommandException)
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, CancellationToken.None);
        return Results.Problem("tmux could not apply the topology change.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        await audit.WriteAsync(action, UserId(context.User), target, false, CancellationToken.None);
        throw;
    }
}

public partial class Program;
