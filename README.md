# tmuxctl

A self-hosted control center for viewing and interacting with tmux sessions.
The existing iPhone-first PWA provides observation-oriented mobile controls;
the in-progress Photino desktop companion provides a conventional
keyboard-and-mouse xterm.js interface on Ubuntu and macOS. The ASP.NET Core
service runs as the same non-root Linux user that owns tmux.

The production default is deliberately closed: loopback-only HTTP,
authentication required, no configured key, and no allowed WebSocket origin.
The supported Compose deployments either terminate HTTPS in Kestrel or use
Tailscale Serve, and publish its Serve backend only on host loopback.

## What is included

- Machine-delimited tmux inventory, opaque browser-facing identifiers, bounded ANSI-sanitized previews, and conservative rule-based status.
- REST APIs, a shared inventory WebSocket, and a Linux `forkpty` terminal bridge attaching a real tmux client.
- Cookie authentication bootstrapped by a deployment access key, CSRF validation, read/interact/admin policies, origin checks, rate and connection limits, security headers, and JSON-lines auditing.
- React/TypeScript cards with vertical CSS snap, explicit navigation,
  device-local terminal-open recency ordering, live session-name filtering,
  detached-only filtering and highlighting, guarded create-and-open and
  confirmed single-session termination flows, state preservation, quick
  actions, details, realtime reconnect, and offline states.
- Lazy-loaded xterm.js terminal with resize, disconnect/reconnect, tmux-backed
  touch/button history by default, explicit device-local per-session distance-
  and velocity-scaled application/TUI scrolling for swipes and Older/Latest,
  one-shot Ctrl/Alt, mobile shortcut keys, and guarded clipboard paste with a
  Safari fallback.
- Manifest, icons, service worker, offline shell, systemd/nginx examples, and Tailscale guidance.
- Unit, HTTP integration, WebSocket authorization, and isolated real-tmux PTY lifecycle tests.
- Repo-local Tempo skills, contracts, goals, verification, and review records.
- A repo-local `$setup-tmux-mobile` first-run skill with private environment/key
  generation and a host/container tmux compatibility gate.
- Owner-only workspace snapshots plus an explicit in-app restore action. Session,
  window, pane, layout, and working-directory metadata survive reboot; Codex and
  Claude resume with fixed CLI commands while other panes reopen as shells.
- A separate server-hosted `/desktop/` interface and self-contained .NET 10
  Photino shell. The desktop path keeps authentication, CSRF, REST, and terminal
  WebSockets same-origin and keeps one real tmux client attached for each open
  terminal tab without changing the mobile PWA. Switching tabs preserves those
  attachments; closing a tab/window detaches its clients, and transient network
  loss reconnects with bounded exponential backoff. Tmux-authoritative window
  tabs and pane controls support selection, fixed horizontal/vertical splits,
  bounded resize, and guarded close without exposing arbitrary commands.
  Desktop shortcuts include Ctrl+PageUp/PageDown for session tabs,
  Ctrl+Shift+W to detach the active tab, and Ctrl+Shift+C/V for terminal
  selection copy and guarded paste. A pop-out control opens a session in an
  independent native window. Explicit session kill requires typing the exact
  session name; terminal `exit` keeps ordinary tmux semantics.

## Development

Requirements: Linux, .NET 10 SDK, Node.js 20+, npm, and tmux 3.2+.

```bash
npm --prefix src/TmuxMobile.Web ci
npm --prefix src/TmuxMobile.Web run build
dotnet restore
dotnet test
TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 \
  dotnet test tests/TmuxMobile.Infrastructure.Tests --filter Category=LinuxIntegration
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/TmuxMobile.Server
```

Development mode is the only environment where the documented authentication bypass can activate, and it also requires `Authentication:AllowDevelopmentBypass=true`. The checked-in development file enables both conditions. The server listens at `http://127.0.0.1:5179`.

For frontend hot reload, run the server and:

```bash
npm --prefix src/TmuxMobile.Web run dev
```

Open `http://127.0.0.1:5173`. Vite proxies API and WebSocket traffic to the backend.

For the desktop frontend hot-reload server, use:

```bash
npm --prefix src/TmuxMobile.Web run dev:desktop
```

The native client opens a server chooser when launched without arguments. It
stores multiple labels and validated server origins in the operating system's
application-data directory (`~/.config/tmuxctl/profiles.json` on a conventional
Ubuntu setup) with owner-only permissions. It never stores login keys or
terminal content. HTTP is allowed only for loopback development; normal server
URLs must use HTTPS. A URL argument bypasses the chooser for development and
automation:

```bash
dotnet run --project src/TmuxCtl.Desktop -- http://127.0.0.1:5179
```

The desktop login key is sent only to the server's same-origin login endpoint.
Use the desktop sidebar's **Servers** control to return to the native chooser.
Before loading remote UI, the native shell checks a content-free, versioned
server capability endpoint. Older or incompatible servers return to the chooser
with an update message. If the compatible initial page cannot load, the native
shell returns to the chooser after 12 seconds with server, network, and TLS
troubleshooting context.

## Production build

```bash
npm --prefix src/TmuxMobile.Web ci
npm --prefix src/TmuxMobile.Web run build
dotnet publish src/TmuxMobile.Server/TmuxMobile.Server.csproj \
  --configuration Release --output artifacts/publish
./scripts/build-desktop.sh linux-x64
./scripts/build-desktop.sh osx-arm64
```

The frontend build writes separate hashed mobile and desktop assets into the
server's `wwwroot`. The desktop binary still expects an already-running server;
it does not install or launch one. Follow [deployment.md](docs/deployment.md)
for systemd and HTTPS setup. Configuration is documented in
[configuration.md](docs/configuration.md).

The desktop outputs bundle the .NET runtime, so the target machine does not
need a separately installed .NET SDK or runtime. They still use each operating
system's native web view. On Ubuntu 24.04, install the WebKitGTK runtime before
launching the Linux output:

```bash
sudo apt-get install libwebkit2gtk-4.1-0
./artifacts/desktop/linux-x64/tmuxctl
```

The `osx-arm64` output targets Apple Silicon and uses macOS's built-in WebKit.
Source builds are not signed or notarized in this first cut.

## Docker Compose over Tailscale

The preferred production shape is a single non-root container with a
loopback-only host binding behind Tailscale Serve:

For a fresh clone, ask your compatible coding agent to use
`$setup-tmux-mobile`. The skill diagnoses tmux, Docker Compose, and Tailscale;
generates ignored mode-`0600` configuration without displaying the login key;
builds the image with the host's exact tmux release; and requires an isolated
socket compatibility probe before proposing the long-lived start. Docker Engine
with Compose v2 and Tailscale remain user-installed prerequisites.

The lower-level manual path remains available:

```bash
cp deploy/docker/.env.example deploy/docker/.env
# Fill the host-specific IP, MagicDNS name, UID/GID, access key, host tmux
# version/socket, and protected state directories. For Tailscale Serve:
docker compose -f compose.tailscale-serve.yaml \
  --env-file deploy/docker/.env config --quiet
docker compose -f compose.tailscale-serve.yaml \
  --env-file deploy/docker/.env up -d --build
```

See [the Compose guide](deploy/docker/README.md). Missing security-critical
values make Compose fail before startup, and the host mapping never defaults to
`0.0.0.0`. In the Serve profile, direct backend HTTP application traffic is
rejected and the backend port is reachable only from the host. Because Docker
binds loopback rather than the Tailscale interface, container recovery does not
depend on Tailscale having assigned its address first during boot.

## Verification

```bash
./scripts/verify.sh
curl --fail http://127.0.0.1:5179/health/live
curl --fail http://127.0.0.1:5179/health/ready
```

The opt-in Linux PTY tests create unique `tmux -L tmux-mobile-...` servers,
attach through PTYs, verify session survival and mouse-wheel forwarding to an
alternate-screen program, and destroy only those dedicated servers. They never
address the user's default tmux socket. They are opt-in because forking inside a
multi-project VSTest host is nondeterministic; run them as the isolated command
above.

## Documentation

- [Architecture and decisions](docs/architecture.md)
- [Deployment, HTTPS, Tailscale, upgrades, and rollback](docs/deployment.md)
- [Security model and operations](docs/security.md)
- [HTTP and WebSocket API](docs/api.md)
- [Configuration reference](docs/configuration.md)
- [Docker Compose deployment](deploy/docker/README.md)
- [Tempo project contract](SPEC.md)

## Known limitations

- One local tmux host and one owner identity are supported.
- The first desktop cut targets Ubuntu x64 and Apple Silicon macOS. Server
  launch, Intel macOS, Windows, native installers, signing, and published binary
  releases are deferred.
- PTY support is Linux-only and uses a small native `forkpty`/immediate-`exec`
  boundary compiled during build.
- Status is heuristic and intentionally returns `Unknown` when signals are weak.
- Preview polling is cached per active pane; this is not terminal history indexing.
- iPhone Safari, installed-mode, sleep/wake, orientation, and Tailscale network switching still require validation on the target physical device and host.
- Data-protection keys are protected by filesystem permissions, not automatically encrypted at rest. A certificate-backed key encryptor can be added for hosts with managed certificate storage.
- Destructive operations, arbitrary commands, file browsing, process restart,
  recording, notifications, and multiple hosts are intentionally absent. Session
  creation accepts only a validated name and starts tmux's configured default
  command; clients cannot provide a command, path, environment, or tmux options.
- Workspace restore is never automatic at boot and does not preserve terminal
  output, command arguments, environment variables, credentials, SSH
  connections, or process memory. It restores only when tmux is empty and the
  authenticated owner presses Restore in the app.

Recommended next steps after physical-device validation are session favorites/order, read-only identities, rule adapters, and notifications for explicit waiting/error states.
