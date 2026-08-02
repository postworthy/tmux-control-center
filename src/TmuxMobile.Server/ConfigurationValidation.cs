using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Server;

public sealed class SecurityConfigurationValidator(IHostEnvironment environment) :
    IValidateOptions<AuthOptions>, IValidateOptions<SecurityOptions>, IValidateOptions<TmuxOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        if (options.UnsafeAllowInsecureHttp &&
            !options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                "UnsafeAllowInsecureHttp is permitted only with ApiKey authentication.");

        if (options.UnsafeAllowWeakApiKeyForTest &&
            !options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(
                "UnsafeAllowWeakApiKeyForTest is permitted only with ApiKey authentication.");

        if (options.Mode.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment() || !options.AllowDevelopmentBypass)
                return ValidateOptionsResult.Fail("Development authentication requires Development environment and AllowDevelopmentBypass=true.");
        }
        else if (options.Mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment() && !options.UnsafeAllowProductionBypass)
                return ValidateOptionsResult.Fail("Authentication cannot be disabled in production without UnsafeAllowProductionBypass=true.");
        }
        else if (options.Mode.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                return ValidateOptionsResult.Fail("ApiKey authentication requires a non-empty secret.");
            var minimumLength = options.UnsafeAllowWeakApiKeyForTest ? 8 : 24;
            if (options.ApiKey.Length < minimumLength)
                return ValidateOptionsResult.Fail(
                    $"ApiKey authentication requires a secret of at least {minimumLength} characters.");
        }
        else return ValidateOptionsResult.Fail("Authentication mode must be ApiKey, Development, or Disabled.");
        return ValidateOptionsResult.Success;
    }

    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        if (!environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
            return ValidateOptionsResult.Fail("At least one AllowedOrigins entry is required outside development.");
        if (options.AllowedOrigins.Any(x => x == "*"))
            return ValidateOptionsResult.Fail("Wildcard origins are not allowed.");
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
}
