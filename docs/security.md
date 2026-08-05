# Security

This service controls terminals owned by its Linux account and must be treated as a privileged local control plane. Tailscale narrows network reachability; it does not make unsafe application behavior safe.

## Trust model

- Run as the same non-root account that owns the target tmux server. Any successful terminal interaction has that account's effective access. A compromise can therefore affect everything that account can affect.
- Bind ASP.NET Core to loopback unless a deliberate deployment requires otherwise.
- Terminate TLS before any user traffic. Do not expose the HTTP listener to a LAN or public interface.
- Restrict the MagicDNS name/port with Tailscale grants or ACLs to named owner identities and managed devices.
- Keep the deployment access key in a root-readable systemd environment file and use at least 32 random bytes. It establishes a cookie; it is never browser-persisted.

## Implemented controls

- No `/bin/sh -c`, command interpolation, arbitrary process launch, arbitrary filesystem endpoint, restart, or destructive API.
- Process executable and arguments remain separate. Every session/pane target is rediscovered from tmux and compared through an opaque identifier immediately before use.
- Rename values use Unicode normalization, a 64-character allowlist, and typed bodies. Text has NUL and length checks and uses literal tmux input.
- Process timeouts, cancellation, separate bounded stdout/stderr drains, capture line/byte limits, HTTP body limits, WebSocket frame limits, terminal idle timeout, global/per-user terminal leases, identity/IP-partitioned request limits, and per-connection terminal message/byte limits.
- Secure/HttpOnly/SameSite production cookies, persistent Data Protection keys, constant-time access-key comparison, antiforgery tokens, explicit policies, and failed-login auditing. Disabled authentication is rejected in every environment. The loopback-only Development bypass uses a non-Secure development antiforgery cookie so actions can be exercised over local HTTP; that exception cannot activate outside the Development environment.
- Strict same-origin frontend usage and configured WebSocket origins. Forwarded headers are disabled by default and, when enabled, trust only listed proxy IPs.
- One-year HSTS on production HTTPS, same-origin-only CSP connectivity, frame denial, MIME sniffing denial, no-referrer, a restrictive browser permissions policy, and `Cache-Control: no-store` on APIs. `style-src 'unsafe-inline'` remains narrowly required for xterm.js runtime styles; scripts do not receive an inline exception.
- Terminal contents and keystrokes are absent from application logs and audits. Audits record actor, operation, opaque target, success, and time. Failed allowed interactions are recorded, and a sink failure is logged separately without changing an already-applied action response.
- Clipboard text is read only after the user taps Paste, remains in ephemeral
  terminal component state, and is cleared after send or cancel. Multiline and
  large pastes require confirmation, no Enter is appended, and serialized input
  is chunked below the WebSocket message limit.
- Terminal history messages accept only older, newer, or latest plus a bounded
  page count. The server re-resolves the opaque session ID and builds fixed
  tmux copy-mode argument arrays. This remains the default swipe path and never
  writes bytes into the PTY input stream. A per-connection token bucket closes
  burst senders before they can amplify tmux process creation.
- Application Scroll is an explicit default-off terminal control. While enabled,
  one completed vertical swipe creates at most three fixed-direction wheel
  events through xterm's negotiated mouse protocol. Those events are foreground
  application input and may have context-dependent effects; the mode is visibly
  indicated, is not stored, and resets on terminal entry, exit, or connection
  loss. It adds no WebSocket message that accepts caller-supplied escape
  sequences, text, or mouse coordinates.
- Service worker exclusions prevent caching APIs, health responses, captured output, or socket traffic.
- Startup validation rejects disabled authentication and the legacy bypass in all environments, short/missing API keys, wildcard or non-origin values, insecure production origins, mismatched Hosts, unsafe proxy/listener combinations, relative tmux paths, invalid socket names, and invalid prefixes.

## Secret and file permissions

Recommended ownership:

```bash
sudo chown -R tmuxuser:tmuxuser /opt/tmux-mobile
sudo chmod 0750 /opt/tmux-mobile
sudo install -o root -g tmuxuser -m 0640 deploy/systemd/tmux-mobile.env.example /etc/tmux-mobile.env
sudo install -d -o tmuxuser -g tmuxuser -m 0700 /var/lib/tmux-mobile/keys /var/log/tmux-mobile
```

Set `DataProtection__KeysDirectory=/var/lib/tmux-mobile/keys` and `Audit__Destination=/var/log/tmux-mobile/audit.jsonl`. Filesystem permissions protect cookie keys; use a certificate-backed ASP.NET Core key encryptor if the host threat model requires at-rest cryptographic protection.

On Linux the audit parent must be owner-only (`0700`) and an existing file must
be owner-readable/writable only (`0600`). Startup fails if either grants group
or other access. Audit append cancellation, I/O, authorization, or permission
failure is emitted as a structured application error; the action result is not
rewritten, which avoids encouraging duplicate retries.

Generate a key without putting it in shell history where possible:

```bash
openssl rand -base64 48
```

Do not place the key in the repository, URL, browser local storage, or reverse-proxy logs.

## Reverse proxy

The supplied nginx configuration accepts HTTPS and proxies only to loopback. Set `ForwardedHeaders__Enabled=true` and keep `KnownProxies` limited to loopback. If binding Kestrel beyond loopback, do not trust forwarded headers from arbitrary peers.

Set top-level `AllowedHosts` to the exact MagicDNS hostname and configure `Security__AllowedOrigins__0=https://that-name`. A mismatched Host is rejected by ASP.NET Core Host Filtering and a mismatched WebSocket Origin is rejected during upgrade.

## Tailscale policy

Use grants/ACLs to allow only the owner identity or device tag to reach the tmux host on the chosen HTTPS port. Do not use a broad `*:*` destination. Device posture checks can further require managed, updated devices. Policy syntax evolves; validate the example in [deployment.md](deployment.md) against the current Tailscale admin console before applying it.

## Container boundary

The Compose deployment requires `TAILSCALE_IP` in the host-side port mapping.
Although Kestrel listens on all interfaces inside its private container
namespace, Docker publishes it only on that exact host address. Do not remove
the address portion of the mapping.

The container drops Linux capabilities, enables `no-new-privileges`, uses a
read-only root filesystem, and runs as the tmux owner's numeric non-root
UID/GID. Mount only that user's tmux socket directory—not host `/tmp`, `/`,
`/var/run`, or the Docker socket. TLS keys, authentication configuration,
audits, and data-protection keys remain outside the image.

The writable `/tmp` is an in-memory, `noexec`, `nosuid`, `nodev` tmpfs. The
specific host tmux socket directory is a nested mount; host `/tmp` is never
shared wholesale.

Because tmux uses a client/server protocol over the mounted Unix socket, confirm
that the image's tmux client can talk to the host server after host or image
upgrades. A protocol mismatch is a readiness failure, not a reason to grant the
container broader host access.

### Temporary HTTP smoke mode

`compose.http-test.yaml` is only for pre-TLS tailnet testing. It requires the
normal API key plus `Authentication:UnsafeAllowInsecureHttp=true`, uses
HttpOnly/SameSite=Strict cookies without the `Secure` attribute, and emits a
startup warning. The configuration validator rejects this switch with disabled
or Development authentication.

That test Compose file also enables
`Authentication:UnsafeAllowWeakApiKeyForTest=true`. <!-- gitleaks:allow --> This permits a deliberately
simple temporary key of at least eight characters. The production HTTPS Compose
file never enables it and retains the 24-character minimum.

Bind this mode only to the machine's exact Tailscale IP. HTTP is not the final
deployment posture; stop it and return to `compose.yaml` after tailnet HTTPS is
available.

### Tailscale Serve HTTPS test mode

`compose.tailscale-serve.yaml` keeps Kestrel on the exact Tailscale-IP HTTP
backend required by the local Serve proxy, but configures the application for
the public tailnet-only HTTPS hostname and origin. It uses the normal Secure
`__Host-` cookies and permits the temporary eight-character test key through
the explicit weak-key switch. Direct HTTP is not the browser-facing URL.

Tailscale Serve remains tailnet-only and tailnet access rules still apply. Do
not replace Serve with Funnel for this control service.

Trusted forwarded headers run before the HTTPS boundary. The application then
rejects ordinary direct HTTP application traffic with 426; anonymous liveness
and loopback readiness are the only exceptions. Continue to omit the backend
port from grants/ACLs because application rejection is defense in depth, not a
replacement for network policy.

## Audit and incident response

Inspect:

```bash
journalctl -u tmux-mobile --since today
sudo tail -n 100 /var/log/tmux-mobile/audit.jsonl
```

Rotate by renaming the file and allowing the next append to create a new `0600`
file, or stop the service during copy/truncate rotation. Keep the directory
`0700`, retain audit files according to local policy, and alert on
`Audit sink failed` or startup permission-validation errors. Never configure a
log shipper that broadens local permissions or records terminal streams.

On suspected key disclosure: restrict the Tailscale rule, stop the unit, replace the access key, remove the Data Protection keys to invalidate all cookies, restart, and inspect audit/system logs. Removing keys logs everyone out and is intentionally disruptive.

## Dependency review

`dotnet restore` uses NuGet vulnerability metadata and `npm audit` should report zero known vulnerabilities before deployment:

```bash
dotnet list package --vulnerable --include-transitive
npm --prefix src/TmuxMobile.Web audit --omit=dev
```

Review and rebuild on a regular patch cadence. Do not enable the production authentication bypass for troubleshooting.
