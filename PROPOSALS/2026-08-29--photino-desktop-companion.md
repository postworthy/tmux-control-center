# Proposal: tmuxctl Photino Desktop Companion

Date: 2026-08-29
Owner: Human Partner and AI Agent
Risk Class: T2
Related Issue/Context: The owner wants a conventional Ghostty/Terminator-style
desktop companion without Electron or the mobile PWA interaction model.
Roadmap Item: C022
Planned Branch: `feat/c022-desktop-photino-client`
Expected Commit Count: 6-10

## Objective

Deliver a source-buildable Ubuntu x64 and Apple Silicon macOS desktop client
that connects to an existing tmuxctl server, presents tmux-owned tabs and
splits through a desktop-first xterm.js interface, and participates accurately
in tmux attachment and session lifecycle.

## Scope

In scope:

- A self-contained .NET 10/Photino app with versioned desktop-specific frontend
  assets and no machine-wide .NET requirement; the feasibility spike will
  determine whether those assets are bundled locally or served by tmuxctl.
- Multiple device-local server profiles containing a label and validated HTTPS
  URL; Tailscale connectivity and the remote server are prerequisites.
- A proven remote authentication/CSRF/origin/WebSocket topology that preserves
  existing server security and stores no plaintext login secret in app settings.
- Session listing, selection, creation, real attachment, detach, reconnect, and
  explicit named-confirmation termination.
- One top-level desktop tab per tmux session, subordinate tabs for tmux windows,
  and real tmux panes for splits, with closed typed operations for topology.
- Bounded stale-client cleanup, desktop keyboard/mouse interaction including
  bounded Ctrl+mouse-wheel terminal text zoom, tests, documentation, rollback,
  and reproducible `linux-x64`/`osx-arm64` source builds.

Out of scope:

- Installing, launching, configuring, or supervising the server, tmux, Docker,
  Tailscale, or host recovery service from the desktop app.
- Intercepting `exit`, killing a session when a tab closes, caller-controlled
  tmux/shell commands, terminal-content persistence, or weakening server auth.
- Replacing the PWA, reusing its mobile controls, native terminal rendering,
  Electron, Windows, Intel macOS, broad Linux portability, collaboration, or a
  single server managing multiple tmux hosts.
- `.deb`, `.dmg`, signing, notarization, app-store delivery, automatic updates,
  and published binary releases.

## Expected Files Touched

- Product contracts, roadmap, architecture/security/build documentation, goal,
  proposal, and review records.
- A new desktop .NET project and desktop-only TypeScript/xterm.js frontend.
- Solution/build scripts and dependency notices.
- Additive typed server contracts, WebSocket lifecycle logic, tmux service
  operations, and corresponding unit/integration tests.
- Optional GitHub Actions build workflow only after approval at the remote
  publication boundary.

## Acceptance Criteria

- [ ] Clean-checkout source builds produce launchable self-contained Ubuntu x64
  and Apple Silicon macOS outputs without a preinstalled .NET runtime.
- [ ] Saved server profiles connect and authenticate safely to an existing
  tmuxctl HTTPS server and recover clearly from auth, TLS, offline, and sleep
  transitions without persisting terminal content or plaintext login secrets.
- [ ] Opening and closing desktop terminals creates and removes only the app's
  real tmux clients, with bounded stale cleanup and no underlying session loss.
- [ ] Sessions, tmux windows, and tmux panes round-trip as session tabs, window
  tabs, and splits across reconnect and the mobile client.
- [ ] Creation, detach, ordinary `exit`, and explicit confirmed session kill
  retain their distinct tmux semantics and operate only on validated targets.
- [ ] Desktop interaction acceptance, including bounded Ctrl+mouse-wheel text
  zoom that leaves unmodified wheel behavior intact, focused security/
  integration tests, canonical verification, documentation, rollback, and
  Change Review pass.

## Verification Plan

Commands:

```bash
./scripts/verify.sh
```

Focused checks will include desktop frontend unit tests and production build,
.NET desktop/core/server tests, isolated real-tmux lifecycle/topology tests,
`dotnet publish` for `linux-x64` and `osx-arm64`, an Ubuntu launch smoke test,
and an actual Apple Silicon launch/attach smoke test on owner-approved hardware
or an approved macOS CI runner.

Pass means every goal criterion has final evidence, the mobile PWA and existing
server deployment remain compatible, canonical verification exits 0, and the
review record finds no blocking issue.

## Change Review Plan

- Review Boundary: merge from `feat/c022-desktop-photino-client` into `main`
- Planned Review Record: `REVIEWS/2026-08-29--photino-desktop-companion.md`
- Reviewer expectation: inspect authentication isolation, dependency licenses,
  fixed tmux operations, attachment ownership/cleanup, cross-platform build
  evidence, mobile compatibility, rollback, and documentation.

## Git Plan

- Branch: `feat/c022-desktop-photino-client`
- Commit sequence: contracts/spike, desktop shell, session thin slice, tmux
  topology, resilience/UX, packaging/docs, and review evidence.
- Commit subject pattern: `feat(desktop): <coherent outcome>`
- Required trailers: `Roadmap: ROADMAP/COMMIT-PLAN.md#C022` and
  `Proposal: PROPOSALS/2026-08-29--photino-desktop-companion.md`.
- Merge and push remain explicit owner-controlled boundaries.

## Decomposition Plan

1. Security/transport feasibility spike — prove one Photino window can use a
   configured remote URL to authenticate, fetch inventory, attach xterm.js over
   WebSocket, and detach without weakening same-origin/CSRF controls — Risk T2.
2. Desktop shell and profiles — self-contained app, settled asset pipeline,
   validated label/URL profiles, settings safety, offline/auth/TLS states — Risk
   T1 after the spike settles the architecture.
3. Session thin slice — list one server's sessions, open one as a real attached
   tmux client in a desktop tab, and detach it on tab/window close — Risk T2.
4. Authoritative topology — typed fixed server operations and desktop mappings
   for tmux windows, panes, selection, resize, create, and close — Risk T2.
5. Lifecycle and desktop interaction — heartbeat/stale cleanup, reconnect,
   multiple windows, shortcuts, selection, clipboard, context menu, sizing,
   bounded Ctrl+mouse-wheel text zoom, and explicit session create/kill
   behavior — Risk T2.
6. Cross-platform delivery — deterministic source builds, Linux/macOS launch
   evidence, documentation, canonical verification, rollback, and review — Risk
   T1 locally; external macOS CI or push requires separate approval.

Thin slice milestone:

- From an Ubuntu desktop app, select a saved server URL, authenticate, list
  sessions, attach one real tmux client in xterm.js, and close the tab so only
  that client detaches while the session remains available in the mobile PWA.

Dependencies and unknowns:

- Photino's precise cookie, local-origin, custom-scheme, and WebSocket behavior
  must be proven against the existing secure-cookie/CSRF/origin policy before
  choosing bundled-local assets versus a server-served desktop entry point.
- Photino native runtime assets and licenses must support self-contained
  `linux-x64` and `osx-arm64` publishing on .NET 10.
- Final Apple Silicon launch evidence needs owner-approved Mac hardware or a
  separately approved remote CI run.

Intentional deferrals:

- Server management, native rendering, installers, signing/notarization, Intel
  macOS, Windows, broad Linux packaging, binary publication, and auto-update.

## Rollback Plan

Keep all new API routes additive and feature-isolated. Remove or disable the
desktop project and revert C022 commits; existing mobile assets, API routes,
Docker deployment, tmux sessions, and recovery service continue unchanged.
Validate rollback with `./scripts/verify.sh` and an existing mobile attach/
detach/session-action smoke test.

## Risks and Mitigations

- Risk: embedded-webview origins conflict with secure cookies, CSRF, or
  WebSockets. Mitigation: make this the first spike; do not add exceptions until
  a least-privilege architecture is demonstrated and reviewed.
- Risk: UI layout diverges from tmux and becomes stale. Mitigation: keep tmux
  authoritative and derive all tabs/splits from refreshed inventory.
- Risk: app crashes leave false attached state. Mitigation: connection-owned
  PTYs, heartbeat expiry, idempotent cleanup, and real-tmux abrupt-loss tests.
- Risk: closing UI kills work. Mitigation: detach only owned clients and test
  session identity plus other attached clients before and after every close path.
- Risk: topology endpoints become arbitrary command execution. Mitigation: a
  closed operation set, opaque inventory targets, strict bounds, fixed argv,
  authorization, CSRF, rate limits, and audits.
- Risk: macOS output compiles but does not run. Mitigation: require actual
  Apple Silicon launch/attach evidence before goal completion.

## Compatibility / Migration Notes

Server changes are additive and the PWA remains the default root/mobile client.
Older servers may reject unsupported desktop topology operations; the desktop
must negotiate capabilities and provide an actionable compatibility message.
No database, session snapshot, or tmux configuration migration is permitted.

## Observability / Debug Notes

Log connection state, server profile label, operation category, attachment ID,
and sanitized failure category. Never log login secrets, terminal input/output,
clipboard content, session content, or raw WebSocket payloads.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-29 when the owner accepted the recommended Photino/
  xterm.js architecture, remote-server boundary, tmux-authoritative mapping,
  Apple Silicon target, source-build delivery, attachment lifecycle, and
  session-action semantics.
