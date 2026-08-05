# Deployment

Run the service as the same non-root Linux user that owns the tmux server. That gives the service the access needed to attach, but a service compromise has all permissions of that account. Use a dedicated account for tmux workloads when practical.

The preferred publishing path is the lightweight Docker Compose deployment in
[`deploy/docker/README.md`](../deploy/docker/README.md). It either terminates
HTTPS in the application container or uses Tailscale Serve, and maps the host
port only on a required Tailscale IP. The existing systemd approaches below
remain supported alternatives.

## Docker Compose on the host tailnet

Copy and complete the safe environment example. For the current Tailscale Serve
shape, validate and start with:

```bash
cp deploy/docker/.env.example deploy/docker/.env
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env config --quiet
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env build
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env up -d
```

The container runs with the tmux owner's numeric UID/GID and mounts that user's
specific tmux socket directory. It does not run its own tmux server. Its
read-only root filesystem receives only writable data-protection/audit mounts
and read-only TLS files. A small in-memory `/tmp` supports PTY and atomic
data-protection operations; the specific host tmux socket directory is mounted
over its matching subdirectory. `TAILSCALE_IP` has no default: omitting it stops
Compose configuration instead of exposing the port on every host interface.

The image installs the distribution tmux client. Confirm compatibility with the
host tmux server using a disposable session before relying on critical
workloads. See the Compose guide for exact TLS, health, upgrade, and rollback
commands.

The systemd unit deliberately leaves `PrivateTmp=false`: tmux normally stores its per-user server socket under `/tmp`, and a private mount namespace would make that socket invisible. Other filesystem hardening remains enabled.

## Build and install

As the tmux owner:

```bash
npm --prefix src/TmuxMobile.Web ci
npm --prefix src/TmuxMobile.Web run build
dotnet test
TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 \
  dotnet test tests/TmuxMobile.Infrastructure.Tests --filter Category=LinuxIntegration
dotnet publish src/TmuxMobile.Server/TmuxMobile.Server.csproj \
  -c Release -o artifacts/publish
```

As an administrator, replace `tmuxuser`:

```bash
sudo install -d -o tmuxuser -g tmuxuser -m 0750 /opt/tmux-mobile
sudo cp -a artifacts/publish/. /opt/tmux-mobile/
sudo chown -R tmuxuser:tmuxuser /opt/tmux-mobile
sudo install -d -o tmuxuser -g tmuxuser -m 0700 /var/lib/tmux-mobile/keys
sudo install -d -o tmuxuser -g tmuxuser -m 0700 /var/log/tmux-mobile
sudo install -o root -g root -m 0644 deploy/systemd/tmux-mobile.service /etc/systemd/system/
sudo install -o root -g tmuxuser -m 0640 deploy/systemd/tmux-mobile.env.example /etc/tmux-mobile.env
sudo systemctl daemon-reload
sudo systemctl enable --now tmux-mobile
```

Edit `/etc/tmux-mobile.env` before first start. Never leave the example access key.

## Approach 1: Kestrel terminates HTTPS

Install a certificate readable by the service account, configure:

```ini
ASPNETCORE_URLS=https://127.0.0.1:5443
ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/tmux-mobile/tls.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=REPLACE
ForwardedHeaders__Enabled=false
Security__AllowedOrigins__0=https://tmux-host.example.ts.net:5443
AllowedHosts=tmux-host.example.ts.net
```

Binding to loopback works when Tailscale Serve forwards locally. If binding directly to a Tailscale address, choose that address explicitly, enforce host firewall/Tailscale grants, and do not use `0.0.0.0`.

## Approach 2: nginx or Tailscale Serve terminates HTTPS

The supplied nginx example proxies to `127.0.0.1:5179` and supports WebSockets:

```bash
sudo install -m 0644 deploy/nginx/tmux-mobile.conf /etc/nginx/sites-available/tmux-mobile
sudo ln -s /etc/nginx/sites-available/tmux-mobile /etc/nginx/sites-enabled/tmux-mobile
sudo nginx -t
sudo systemctl reload nginx
```

Configure the certificate paths and hostname first. Enable forwarded headers with only loopback trusted.

For Tailscale Serve, keep Kestrel loopback-only and configure an HTTPS reverse proxy to the local listener. One typical command is:

```bash
sudo tailscale serve --bg http://127.0.0.1:5179
tailscale serve status
```

Tailscale CLI syntax can change; confirm the current command with `tailscale serve --help`. The external HTTPS origin must match `Security__AllowedOrigins__0`.

For the exact-Tailscale-IP Docker constraint, use
`compose.tailscale-serve.yaml` instead. Docker publishes the backend only on the
required Tailscale IP, while `AllowedHosts` and `Security:AllowedOrigins` accept
only the Serve hostname and HTTPS origin. The browser receives Secure cookies.
Forwarded scheme/address headers are accepted only from the configured proxy
address; after that trust check, unforwarded HTTP application traffic receives
426. The HTTP port exists as the Serve target, not as a second application URL.

For the currently deployed shape:

```bash
sudo tailscale serve --https=8443 --bg http://100.85.13.102:8780
tailscale serve status
```

Replace both addresses with the exact values for the host. Rollback for the
proxy is `sudo tailscale serve --https=8443 off`; capture `tailscale serve
status` before changing it. The current application rollout does not require a
Serve rule change when that mapping already exists.

## Tailscale grants

Conceptual policy restricting only the owner to the HTTPS service port (do not
grant the backend port):

```json
{
  "grants": [
    {
      "src": ["user:owner@example.com"],
      "dst": ["tag:tmux-host"],
      "ip": ["tcp:8443"]
    }
  ],
  "tagOwners": {
    "tag:tmux-host": ["user:owner@example.com"]
  }
}
```

Validate this against the current tailnet policy schema before saving. Prefer a tagged host and named identity over broad groups; add device posture requirements if available.

## iPhone validation

1. Connect the iPhone to the tailnet and open the HTTPS MagicDNS URL in Safari.
2. Sign in, swipe through at least two sessions, and verify the preview does not capture the deck gesture.
3. Use Share → Add to Home Screen. Launch standalone and check safe areas/portrait layout.
4. Open Terminal; test text, Esc/Tab/arrows/Enter, Ctrl-C, Ctrl-D, prefix, and Ctrl/Alt one-shot input.
5. Produce enough output for pane history, drag down to enter tmux history, drag
   up to move newer, then tap Latest and confirm live output resumes.
6. Open a non-critical mouse-aware TUI, confirm swipes still use tmux history by
   default, enable App Scroll, and confirm both swipe directions navigate the
   TUI. Disconnect and reconnect; confirm App Scroll reset to off.
7. Rotate the device and confirm the tmux pane resizes.
8. Return and confirm the same card is visible.
9. Disable connectivity, confirm disconnected state, restore it, and reconnect without reloading.
10. Background for longer than one heartbeat, resume, then test sleep/wake and Wi-Fi/cellular changes.

Do this against non-critical tmux sessions first.

## Health and logs

```bash
curl --fail http://127.0.0.1:5179/health/live
curl --fail http://127.0.0.1:5179/health/ready
systemctl status tmux-mobile
journalctl -u tmux-mobile -f
sudo tail -f /var/log/tmux-mobile/audit.jsonl
```

Readiness may be degraded when no tmux server is running. Application logs intentionally omit terminal contents.
Readiness is loopback-or-authenticated because it executes tmux; liveness is
anonymous, inexpensive, and independently rate limited.

## Upgrade and rollback

Keep versioned releases:

```text
/opt/tmux-mobile/releases/2026-07-30/
/opt/tmux-mobile/current -> releases/2026-07-30/
```

To upgrade: build and test, install a new immutable release directory, stop the unit, repoint `current`, start, check health/logs, then let the PWA advertise its waiting service-worker update. Active terminal connections drop during restart, but tmux sessions do not.

To roll back: stop, repoint `current` to the previous release, start, and verify
health. For Compose, tag the pre-rollout image before replacement and restore
that tag with the same Compose profile. Preserve Data Protection keys and audit
storage across both operations so cookies and evidence remain intact. Verify
the HTTPS URL, direct-backend denial, exact listeners, runtime logs, and local
readiness after upgrade or rollback.
