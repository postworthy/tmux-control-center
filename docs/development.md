# Development guide

[Back to the overview](../README.md)

Use this guide to run or modify tmuxctl locally. To use a deployed server, start
with the [desktop guide](desktop.md) or open its HTTPS address in your browser.
Production setup is covered separately for [Linux](deployment.md) and
[macOS](macos-server.md).

## Prerequisites

Use Ubuntu Linux x64 or Apple Silicon macOS with Git, the .NET 10 SDK, Node/npm
compatible with the frontend dependencies, a C compiler, and tmux. The recovery
helper and canonical checks require Bash 4.3 or newer. On Mac, use Homebrew Bash
and the Xcode Command Line Tools; the [Mac guide](macos-server.md) covers paths.

```bash
git clone https://github.com/postworthy/tmux-control-center.git
cd tmux-control-center
npm --prefix src/TmuxMobile.Web ci
npm --prefix src/TmuxMobile.Web run build
dotnet restore
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/TmuxMobile.Server
```

All commands run from the repository root. Open `http://127.0.0.1:5179`.
The authentication bypass requires both the Development environment and
`Authentication:AllowDevelopmentBypass=true`; the checked-in development
configuration enables the latter. Keep this local development setup separate
from production, where authentication and HTTPS are required.

## Hot reload

Keep the backend running and start the mobile frontend in another terminal:

```bash
npm --prefix src/TmuxMobile.Web run dev
```

Open `http://127.0.0.1:5173`. Vite proxies API and WebSocket traffic to the backend.
For the desktop frontend, use:

```bash
npm --prefix src/TmuxMobile.Web run dev:desktop
```

Open `http://127.0.0.1:5174/desktop/`. To run the native shell against the local
server's built assets:

```bash
dotnet run --project src/TmuxCtl.Desktop -- http://127.0.0.1:5179
```

Omit the URL argument to use the saved-server chooser. See the
[desktop guide](desktop.md) for profiles and interaction controls.

## Build artifacts

The frontend build writes separate hashed mobile and desktop assets to the
server's `wwwroot`. To publish a native server including assets, its .NET runtime,
the PTY library, and recovery helper, run on the target OS and architecture:

```bash
./scripts/build-server.sh
```

Outputs go to `artifacts/server/linux-x64/` or `artifacts/server/osx-arm64/`.
For a framework-dependent server build after building the frontend:

```bash
dotnet publish src/TmuxMobile.Server/TmuxMobile.Server.csproj \
  --configuration Release --output artifacts/publish
```

Build the desktop separately with `./scripts/build-desktop.sh linux-x64` or
`./scripts/build-desktop.sh osx-arm64`. See the [desktop guide](desktop.md) for
runtime dependencies and launcher installation. Generated artifacts stay out of
Git. A build alone does not configure a production deployment; follow the
platform setup guide and [configuration reference](configuration.md).

## Verify changes

Run the repository's canonical gate before submitting changes:

```bash
./scripts/verify.sh
```

It runs shell recovery and delivery checks, .NET tests, frontend type checks and
tests, and all isolated Unix tmux/PTY integration tests. Linux additionally runs
setup, watchdog, and Compose/security checks; macOS explicitly reports those
platform exclusions. Missing native prerequisites fail the gate.

For focused .NET checks:

```bash
dotnet test
TMUX_MOBILE_RUN_UNIX_INTEGRATION=1 \
  dotnet test tests/TmuxMobile.Infrastructure.Tests --filter Category=UnixIntegration
```

Standalone Unix integration tests require that opt-in; the canonical script sets
it automatically. Tests create uniquely named `tmux -L tmux-mobile-...` servers,
verify PTY lifecycle, session survival, and application wheel forwarding, and
clean up only those dedicated servers. They do not target your default socket.
Set `TMUX_MOBILE_TEST_TMUX` to an absolute executable path if tmux is not at
`/usr/bin/tmux` on Linux or `/opt/homebrew/bin/tmux` on macOS.

With a local server running, check health separately:

```bash
curl --fail http://127.0.0.1:5179/health/live
curl --fail http://127.0.0.1:5179/health/ready
```

Automated checks do not replace physical iPhone Safari, installed-mode,
orientation, sleep/wake, Tailscale network switching, or
[desktop acceptance](desktop-acceptance.md). See [project status](../STATUS.md)
for remaining validation. Repository workflow is described in
[AGENTS.md](../AGENTS.md) and the [project contract](../SPEC.md).
