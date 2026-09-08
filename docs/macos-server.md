# Native macOS server and desktop

The server and desktop share source with Linux. Run the server natively on Apple
Silicon to reach the Mac's local tmux socket. The desktop can connect to either a
Linux or Mac server and does not start a server itself. Linux Docker/systemd setup
remains in [deployment.md](deployment.md).

For client installation and everyday controls, see the [desktop guide](desktop.md).
From a fresh checkout, run the commands below at the repository root; see
[development setup](development.md#prerequisites) for the clone command.

## Prerequisites and verification

Use the .NET 10 SDK, Node/npm compatible with the frontend dependencies, Xcode
Command Line Tools (`cc`), tmux and Bash 4.3 or newer. Homebrew Bash is required
for the optional recovery helper; Apple's system Bash is too old. Set an explicit
PATH in noninteractive shells and launchd, for example:

```bash
export PATH="/opt/homebrew/bin:$PATH"
npm --prefix src/TmuxMobile.Web ci
./scripts/verify.sh
```

The canonical gate runs real isolated tmux/PTY tests on both OSes. Override
`TMUX_MOBILE_TEST_TMUX` with an existing absolute executable path if tmux is not
`/opt/homebrew/bin/tmux` on Mac or `/usr/bin/tmux` on Linux. Linux deployment checks
are required on Linux; the Mac gate explicitly reports their platform exclusion.
Build native server artifacts on the target OS/architecture. Cross-publishing a
server RID from a different compiler host is rejected. Existing desktop
cross-publishing is unaffected, but launch verification requires the target OS.

## Build and configure the server

```bash
./scripts/build-server.sh
./scripts/build-desktop.sh osx-arm64
```

Outputs are `artifacts/server/osx-arm64/` and
`artifacts/desktop/osx-arm64/tmuxctl.app`. The server includes its .NET runtime,
compiled PTY library, current mobile/desktop assets and recovery helper. Build
outputs are not source changes to commit. The frontend build regenerates the
server's wwwroot; do not replace it with older MacBook-generated bundles.

Create a separate owner-private state directory, such as
`~/Library/Application Support/tmuxctl-server`, with mode `0700`. Store an
`appsettings.json` there with mode `0600`, containing the settings below. Replace
both hostname entries and generate a private random login key with at least 32
characters; never check this configuration into Git.

```json
{
  "Urls": "http://127.0.0.1:5179",
  "AllowedHosts": "YOUR-MAC.YOUR-TAILNET.ts.net",
  "Tmux": { "ExecutablePath": "/opt/homebrew/bin/tmux" },
  "Authentication": { "Mode": "ApiKey", "ApiKey": "REPLACE-WITH-A-PRIVATE-RANDOM-KEY" },
  "Security": { "AllowedOrigins": ["https://YOUR-MAC.YOUR-TAILNET.ts.net"], "ExternalHttpsTermination": true },
  "ForwardedHeaders": { "Enabled": true, "KnownProxies": ["127.0.0.1", "::1"] },
  "Audit": { "Destination": "logs/audit.jsonl" },
  "DataProtection": { "KeysDirectory": "data-protection" },
  "WorkspaceRecovery": { "Enabled": false }
}
```

Run as the same user that owns tmux, with the private state directory as content
root and the published assets as web root. Substitute absolute paths:

```bash
/ABSOLUTE/PUBLISH/TmuxMobile.Server \
  --contentRoot '/ABSOLUTE/PRIVATE/STATE' \
  --webroot /ABSOLUTE/PUBLISH/wwwroot
```

Use `ASPNETCORE_ENVIRONMENT=Production`. Configure Tailscale Serve separately to
forward HTTPS to `http://127.0.0.1:5179`, retaining application authentication.
Verify native DNS resolution and the exact configured origin. The application
must reject direct HTTP application traffic; health endpoints remain local
liveness/readiness probes. Check `/health/live` and `/health/ready`. No setup
command here changes Tailscale or starts a persistent service automatically.

New audit/key/recovery directories use owner-only Unix permissions. Existing
Linux deployments retain their paths and defaults. When migrating existing state,
check directory modes `0700` and file modes `0600`, including keys, rather than
relying on the shell's umask; existing key directories are not silently chmodded.
The operator must review existing macOS ACLs as well as mode bits.

## Optional per-user launchd supervision

Copy and edit `deploy/launchd/com.tmuxctl.server.plist.example` outside the
repository. Replace every `/ABSOLUTE/...` placeholder, and keep the result private
under `~/Library/LaunchAgents/com.tmuxctl.server.plist`. The template has an
explicit working directory/PATH, private umask, restart throttling, and
`AbandonProcessGroup` so launchd does not kill unrelated tmux descendants.
The server's own PTY cleanup still detaches its owned clients.

Validate with `plutil -lint` before the operator loads it using `launchctl bootstrap
gui/$(id -u) PATH-TO-PLIST`. Use `launchctl bootout` for that specific job to stop
it. Do not run the server as root or load both old and new server jobs. Check logs,
health and tmux attachment/process survival after start and stop. Service
installation and production restart are separate operator actions.

## Optional workspace recovery

The source-published server includes `recovery/tmux-workspace-recovery.sh`,
`workspace-platform.sh` and the host-built `tmux-workspace-lock`. Keep them together
as real files. The daemon needs Bash 4.3+, an explicit tmux executable and the same
owner-private control directory configured in the server. It uses the unchanged
version-1 metadata format and restores only after an explicit app request.

Use the companion `com.tmuxctl.workspace.plist.example`, substituting publish and
state paths. Configure the server's `WorkspaceRecovery.Enabled=true` and an
absolute `WorkspaceRecovery.ControlDirectory` matching `TMUX_WORKSPACE_STATE_DIR`.
If using a named tmux socket, set both `Tmux.SocketName` and
`TMUX_WORKSPACE_SOCKET_NAME` identically. The native helper locks inherited fd 9;
a second daemon cannot acquire the same lock, and kernel process exit releases it.
No GNU coreutils or third-party flock executable is needed on macOS.

Stopping this daemon saves metadata and leaves tmux running. Boot does not
restore anything. An existing live workspace blocks restore, malformed snapshots
are rejected, and only the existing fixed Codex/Claude resume operations may run.
Do not install `auto-tmux-session.sh` as part of this migration; it is an unrelated
personal shell preference.

## Desktop acceptance and rollback

Launch the `.app` on the Mac, select a saved HTTPS server, and follow
[desktop-acceptance.md](desktop-acceptance.md) against both server platforms.
Check Dock identity, Command+C and Ctrl+Shift+C, external clipboard readback,
guarded paste, fullscreen/resize, sleep/reconnect and detach. Test the Linux
client against both servers and the mobile PWA against the Mac server too.
A successful build or PTY test does not prove these physical interactions.

Retain the previous source/publish directory and service configuration. Roll back
only the app/recovery jobs owned by this deployment; preserve profiles, audit,
keys, workspace metadata and tmux sessions. The source migration preserves the
MacBook's pre-integration changes in a private backup and keeps unrelated local
files. See the C023 goal/review for actual verification and migration evidence.
