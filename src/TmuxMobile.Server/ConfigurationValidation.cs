using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Server;

public sealed class SecurityConfigurationValidator(IHostEnvironment environment, IConfiguration configuration) :
    IValidateOptions<AuthOptions>, IValidateOptions<SecurityOptions>, IValidateOptions<TmuxOptions>,
    IValidateOptions<ForwardedHeaderSettings>, IValidateOptions<WorkspaceRecoveryOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        if (options.UnsafeAllowProductionBypass)
            return ValidateOptionsResult.Fail(
                "UnsafeAllowProductionBypass is no longer supported; production authentication cannot be disabled.");

        if (options.UnsafeAllowInsecureHttp &&
            !options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                "UnsafeAllowInsecureHttp is permitted only with ApiKey authentication.");

        if (options.UnsafeAllowWeakApiKeyForTest &&
            !options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                "UnsafeAllowWeakApiKeyForTest is permitted only with ApiKey authentication.");

        if ((options.UnsafeAllowInsecureHttp || options.UnsafeAllowWeakApiKeyForTest) &&
            options.UnsafeTestProfileAcknowledgement != AuthOptions.TailnetTestAcknowledgement)
            return ValidateOptionsResult.Fail(
                $"Unsafe test switches require UnsafeTestProfileAcknowledgement={AuthOptions.TailnetTestAcknowledgement}.");

        if (options.Mode.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment() || !options.AllowDevelopmentBypass)
                return ValidateOptionsResult.Fail("Development authentication requires Development environment and AllowDevelopmentBypass=true.");
        }
        else if (options.Mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail("Disabled authentication is not supported in any environment.");
        else if (options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                return ValidateOptionsResult.Fail("ApiKey authentication requires a non-empty secret.");
            var minimumLength = options.UnsafeAllowWeakApiKeyForTest ? 8 : 24;
            if (options.ApiKey.Length < minimumLength)
                return ValidateOptionsResult.Fail(
                    $"ApiKey authentication requires a secret of at least {minimumLength} characters.");
        }
        else return ValidateOptionsResult.Fail("Authentication mode must be ApiKey or Development.");
        return ValidateOptionsResult.Success;
    }

    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        if (!environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
            return ValidateOptionsResult.Fail("At least one AllowedOrigins entry is required outside development.");

        var auth = configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new();
        var hosts = (configuration["AllowedHosts"] ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!environment.IsDevelopment() &&
            (hosts.Length == 0 || hosts.Any(x => x is "*" or "+")))
            return ValidateOptionsResult.Fail("AllowedHosts must contain exact host names outside development.");

        foreach (var origin in options.AllowedOrigins)
        {
            if (!TryParseExactOrigin(origin, out var uri))
                return ValidateOptionsResult.Fail($"Allowed origin '{origin}' must be an exact absolute HTTP(S) origin.");
            if (!environment.IsDevelopment() && !auth.UnsafeAllowInsecureHttp && uri.Scheme != Uri.UriSchemeHttps)
                return ValidateOptionsResult.Fail($"Allowed origin '{origin}' must use HTTPS outside an unsafe HTTP test profile.");
            if (!environment.IsDevelopment() && !hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                return ValidateOptionsResult.Fail($"Allowed origin host '{uri.Host}' is absent from AllowedHosts.");
        }

        if (options.ExternalHttpsTermination &&
            options.AllowedOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                                                 uri.Scheme != Uri.UriSchemeHttps))
            return ValidateOptionsResult.Fail("ExternalHttpsTermination requires HTTPS allowed origins.");

        var urls = (configuration["Urls"] ?? configuration["ASPNETCORE_URLS"] ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!environment.IsDevelopment() && urls.Any(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) &&
            !options.ExternalHttpsTermination && !auth.UnsafeAllowInsecureHttp)
            return ValidateOptionsResult.Fail(
                "Production HTTP listeners require ExternalHttpsTermination=true or the acknowledged unsafe HTTP test profile.");
        return ValidateOptionsResult.Success;
    }

    public ValidateOptionsResult Validate(string? name, ForwardedHeaderSettings options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        if (options.KnownProxies.Length == 0 && options.KnownProxyHosts.Length == 0)
            return ValidateOptionsResult.Fail("Forwarded headers require at least one explicit known proxy.");
        foreach (var proxy in options.KnownProxies)
            if (!System.Net.IPAddress.TryParse(proxy, out _))
                return ValidateOptionsResult.Fail($"ForwardedHeaders known proxy '{proxy}' is not an IP address.");
        foreach (var host in options.KnownProxyHosts)
            if (string.IsNullOrWhiteSpace(host) || Uri.CheckHostName(host) != UriHostNameType.Dns)
                return ValidateOptionsResult.Fail($"ForwardedHeaders known proxy host '{host}' is not a DNS name.");
        return ValidateOptionsResult.Success;
    }

    public ValidateOptionsResult Validate(string? name, TmuxOptions options)
    {
        if (!Path.IsPathFullyQualified(options.ExecutablePath))
            return ValidateOptionsResult.Fail("Tmux executable path must be absolute.");
        if (options.SocketName is { Length: > 64 } ||
            options.SocketName?.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_')) == true)
            return ValidateOptionsResult.Fail("Tmux socket name contains unsupported characters.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(options.Prefix, @"^C-[A-Za-z]$"))
            return ValidateOptionsResult.Fail("Tmux prefix must use the C-x form.");
        return ValidateOptionsResult.Success;
    }

    public ValidateOptionsResult Validate(string? name, WorkspaceRecoveryOptions options)
    {
        if (!options.Enabled) return ValidateOptionsResult.Success;
        if (!Path.IsPathFullyQualified(options.ControlDirectory))
            return ValidateOptionsResult.Fail("Enabled workspace recovery requires an absolute control directory.");
        return ValidateOptionsResult.Success;
    }

    private static bool TryParseExactOrigin(string value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(parsed.Host) || !string.IsNullOrEmpty(parsed.UserInfo) ||
            !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment) ||
            parsed.AbsolutePath != "/") return false;
        var canonical = $"{parsed.Scheme}://{parsed.Authority}";
        if (!string.Equals(value, canonical, StringComparison.OrdinalIgnoreCase)) return false;
        uri = parsed;
        return true;
    }
}
