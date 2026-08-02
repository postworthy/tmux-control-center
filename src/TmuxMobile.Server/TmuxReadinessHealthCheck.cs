using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TmuxMobile.Core;

namespace TmuxMobile.Server;

public sealed class TmuxReadinessHealthCheck(
    IProcessRunner runner,
    IOptions<TmuxOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.Value.ExecutablePath))
            return HealthCheckResult.Unhealthy("The configured tmux executable does not exist.");
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.Value.SocketName))
            args.AddRange(["-L", options.Value.SocketName]);
        args.AddRange(["list-sessions", "-F", "#{session_id}"]);
        try
        {
            var result = await runner.RunAsync(new(options.Value.ExecutablePath, args,
                TimeSpan.FromSeconds(options.Value.ProcessTimeoutSeconds), 4096, "health.tmux"), cancellationToken);
            if (result.ExitCode == 0) return HealthCheckResult.Healthy();
            if (result.StandardError.Contains("no server running", StringComparison.OrdinalIgnoreCase) ||
                result.StandardError.Contains("failed to connect", StringComparison.OrdinalIgnoreCase))
                return HealthCheckResult.Degraded("tmux is executable, but no tmux server is running.");
            return HealthCheckResult.Unhealthy("tmux query failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("tmux readiness query failed.", exception);
        }
    }
}
