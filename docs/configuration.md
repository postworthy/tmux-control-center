# Configuration

ASP.NET Core configuration applies in this order: checked-in JSON, environment-specific JSON, environment variables, then command-line settings. In environment variables, use `__` for nesting. The production service should use an environment file with secrets and host-specific values.

| Setting | Default | Notes |
|---|---:|---|
| `Urls` | `http://127.0.0.1:5179` | Loopback by default. HTTPS can be configured directly with Kestrel certificate settings. |
| `AllowedHosts` | `localhost;127.0.0.1` | Semicolon-separated Host allowlist. Set the exact MagicDNS/DNS name. |
| `Tmux:ExecutablePath` | `/usr/bin/tmux` | Must be absolute. |
| `Tmux:SocketName` | null | Optional `tmux -L` socket; letters, digits, `_`, `-`, max 64. |
| `Tmux:PollingIntervalSeconds` | 3 | Shared metadata poll, 1–60 seconds. |
| `Tmux:PreviewRefreshIntervalSeconds` | 10 | Per-pane preview cache interval, 2–300 seconds. |
| `Tmux:MaxCaptureLines` | 500 | Server clamp, 10–5000. |
| `Tmux:MaxCaptureBytes` | 131072 | Retained process/capture output, 1 KiB–1 MiB. |
| `Tmux:ProcessTimeoutSeconds` | 5 | Every tmux subprocess, 1–60 seconds. |
| `Tmux:CardPreviewLines` | 80 | Normal card capture depth. |
| `Tmux:Prefix` | `C-b` | Configurable control-letter prefix exposed to the shortcut bar. |
| `Authentication:Mode` | `ApiKey` | `ApiKey` or development-only `Development`. Disabled authentication is rejected in every environment. |
| `Authentication:ApiKey` | unset | Required in `ApiKey` mode, minimum 24 characters; use 32+ random bytes. |
| `Authentication:AllowDevelopmentBypass` | false | Works only in the Development environment. |
| `Authentication:UnsafeAllowProductionBypass` | false | Legacy setting retained only for fail-closed validation; `true` is always rejected. |
| `Authentication:UnsafeAllowInsecureHttp` | false | Test-only: permits non-Secure cookies over HTTP, but only with API-key authentication. Bind solely to a trusted Tailscale IP and remove after testing. |
| `Authentication:UnsafeAllowWeakApiKeyForTest` | false | Test-only: reduces the API-key minimum from 24 to 8 characters, and only in API-key mode. Remove after temporary validation. |
| `Authentication:UnsafeTestProfileAcknowledgement` | unset | Must equal `TAILNET_TEST_ONLY` whenever either unsafe test switch is enabled. |
| `Security:AllowedOrigins` | empty | Exact HTTPS origins required outside development; never wildcard. |
| `Security:ExternalHttpsTermination` | false | Requires HTTPS origins. After trusted forwarded headers run, non-HTTPS application traffic receives 426; anonymous liveness and loopback readiness remain available. |
| `Security:MaxRequestBodyBytes` | 65536 | Kestrel request body cap. |
| `Security:MaxWebSocketMessageBytes` | 16384 | Complete client terminal message cap. |
| `Security:MaxTerminalConnections` | 4 | Global active terminal limit. |
| `Security:MaxTerminalConnectionsPerUser` | 2 | Per-identity active terminal limit. |
| `Security:TerminalIdleTimeoutMinutes` | 30 | PTY cleanup after no browser input. |
| `Security:MaxTerminalInputMessagesPerSecond` | 64 | Per-connection terminal-input message bucket. |
| `Security:MaxTerminalInputBytesPerSecond` | 262144 | Per-connection terminal-input byte bucket. |
| `ForwardedHeaders:Enabled` | false | Enable only behind a known local proxy. |
| `ForwardedHeaders:KnownProxies` | loopback v4/v6 | Explicit IPs allowed to set forwarded scheme/address. |
| `Audit:Destination` | `logs/audit.jsonl` | Use an absolute production path beneath an owner-only directory. Linux startup rejects group/other-accessible audit storage. |
| `DataProtection:KeysDirectory` | `data-protection` | Persistent cookie keys; use an absolute protected production path. |
| `WorkspaceRecovery:Enabled` | false | Enables status and the explicit app-triggered restore request bridge. Compose profiles enable it. |
| `WorkspaceRecovery:ControlDirectory` | `workspace-recovery` | Must be absolute when enabled and owner-private (`0700`); snapshot, request, and status files are `0600`. The host helper and app container must share this exact directory. |
| `DOTNET_GCNoAffinitize` | `1` in the image | Keeps server-GC threads schedulable across the container's available CPUs instead of hard-pinning each thread to one CPU. |
| `TMUX_MOBILE_WATCHDOG_STARTUP_FAILURES` | `12` | Image health watchdog failure budget before first liveness success; integer 1–100. |
| `TMUX_MOBILE_WATCHDOG_STEADY_FAILURES` | `6` | Failure budget after first liveness success; integer 1–100. At the threshold the watchdog terminates only the validated app child so Docker can restart it. |
| `Status:IdleAfterMinutes` | 10 | Conservative inactivity threshold. |
| `Status:WaitingPatterns` | built-in examples | Case-insensitive literal patterns. |
| `Status:CompletedPatterns` | built-in examples | Case-insensitive literal patterns. |
| `Status:FailurePatterns` | built-in examples | Evaluated first. Keep specific to reduce false positives. |
| `Status:ShellCommands` | bash/zsh/fish/sh | Foreground commands treated as a shell prompt signal. |

Example:

```ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5179
AllowedHosts=tmux-host.example.ts.net
Authentication__Mode=ApiKey
Authentication__ApiKey=REPLACE_WITH_RANDOM_SECRET
Security__AllowedOrigins__0=https://tmux-host.example.ts.net
ForwardedHeaders__Enabled=true
ForwardedHeaders__KnownProxies__0=127.0.0.1
DataProtection__KeysDirectory=/var/lib/tmux-mobile/keys
Audit__Destination=/var/log/tmux-mobile/audit.jsonl
WorkspaceRecovery__Enabled=true
WorkspaceRecovery__ControlDirectory=/var/lib/tmux-mobile/workspace
```

For direct Kestrel HTTPS, set `ASPNETCORE_URLS=https://127.0.0.1:5443` (or an explicitly chosen Tailscale address) plus `ASPNETCORE_Kestrel__Certificates__Default__Path` and `...__Password`. Do not set `0.0.0.0` casually; firewall and Tailscale policy must match the explicit exposure.
