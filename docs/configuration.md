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
| `Authentication:Mode` | `ApiKey` | `ApiKey`, `Development`, or `Disabled`. |
| `Authentication:ApiKey` | unset | Required in `ApiKey` mode, minimum 24 characters; use 32+ random bytes. |
| `Authentication:AllowDevelopmentBypass` | false | Works only in the Development environment. |
| `Authentication:UnsafeAllowProductionBypass` | false | High-friction emergency override; never recommended. |
| `Authentication:UnsafeAllowInsecureHttp` | false | Test-only: permits non-Secure cookies over HTTP, but only with API-key authentication. Bind solely to a trusted Tailscale IP and remove after testing. |
| `Authentication:UnsafeAllowWeakApiKeyForTest` | false | Test-only: reduces the API-key minimum from 24 to 8 characters, and only in API-key mode. Remove after temporary validation. |
| `Security:AllowedOrigins` | empty | Exact HTTPS origins required outside development; never wildcard. |
| `Security:MaxRequestBodyBytes` | 65536 | Kestrel request body cap. |
| `Security:MaxWebSocketMessageBytes` | 16384 | Complete client terminal message cap. |
| `Security:MaxTerminalConnections` | 4 | Global active terminal limit. |
| `Security:MaxTerminalConnectionsPerUser` | 2 | Per-identity active terminal limit. |
| `Security:TerminalIdleTimeoutMinutes` | 30 | PTY cleanup after no browser input. |
| `ForwardedHeaders:Enabled` | false | Enable only behind a known local proxy. |
| `ForwardedHeaders:KnownProxies` | loopback v4/v6 | Explicit IPs allowed to set forwarded scheme/address. |
| `Audit:Destination` | `logs/audit.jsonl` | Use an absolute protected production path. |
| `DataProtection:KeysDirectory` | `data-protection` | Persistent cookie keys; use an absolute protected production path. |
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
```

For direct Kestrel HTTPS, set `ASPNETCORE_URLS=https://127.0.0.1:5443` (or an explicitly chosen Tailscale address) plus `ASPNETCORE_Kestrel__Certificates__Default__Path` and `...__Password`. Do not set `0.0.0.0` casually; firewall and Tailscale policy must match the explicit exposure.
