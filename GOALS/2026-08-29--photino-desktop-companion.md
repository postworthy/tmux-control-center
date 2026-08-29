# Goal: tmuxctl Photino Desktop Companion

Status: active
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-29
Proposal: `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Review Boundary: merge from `feat/c022-desktop-photino-client` into `main`

## Outcome

An owner can build a self-contained tmuxctl desktop client from the repository
for Ubuntu x64 or Apple Silicon macOS, connect it by saved HTTPS URL to an
already-running tmuxctl server over Tailscale, and manage real tmux
sessions/windows/panes through a conventional desktop xterm.js experience whose
attachment, detach, exit, and explicit-kill behavior agrees with tmux and the
existing mobile PWA.

## Non-Goals

- Do not install, launch, configure, or supervise the server, Docker, tmux,
  Tailscale, or workspace recovery from the desktop app.
- Do not replace or restyle the mobile PWA, intercept terminal `exit`, persist
  terminal content, accept arbitrary tmux/shell commands, or weaken auth.
- Do not build a native terminal renderer or use Electron.
- Do not add Windows, Intel macOS, broad Linux portability, `.deb`/`.dmg`
  installers, signing/notarization, app stores, auto-update, or published binary
  releases.

## Acceptance Criteria

- [ ] AC1 — Clean-checkout documented commands produce self-contained
  `linux-x64` and `osx-arm64` desktop outputs; Ubuntu launches without installed
  .NET and actual Apple Silicon hardware or an approved macOS CI runner proves
  the macOS app launches and reaches its connection screen.
  - Evidence: pending
- [ ] AC2 — Multiple label/HTTPS-URL profiles validate and persist locally; one
  selected profile connects through the existing protected login flow and
  handles invalid URL, TLS, auth, offline, sleep, and reconnect states without
  persisting a plaintext login secret or terminal content.
  - Evidence: pending
- [ ] AC3 — Opening a listed session creates a real tmux client attachment;
  closing its tab/window or abrupt client loss removes only app-owned clients
  within the documented heartbeat bound while the session and other clients
  remain alive and inventory/mobile attached state converges.
  - Evidence: pending
- [ ] AC4 — One desktop session tab presents authoritative tmux window tabs and
  pane splits; create/select/resize/close operations survive reconnect and are
  visible from the PWA or another tmux client.
  - Evidence: pending
- [ ] AC5 — Session listing, selection, creation, detach, and named-confirmation
  kill work only on inventory-resolved targets; closing UI never kills a
  session, and typed `exit` retains ordinary tmux pane/window/session behavior.
  - Evidence: pending
- [ ] AC6 — Desktop keyboard navigation, independent windows, focus, selection,
  copy/paste, context menus, resize, and reconnect pass automated interaction
  checks and owner Ubuntu acceptance without mobile cards, swipe navigation,
  touch shortcuts, or oversized mobile controls.
  - Evidence: pending
- [ ] AC7 — Every new operation rejects unauthorized, cross-origin,
  rate-limited, stale, malformed, and caller-command-bearing requests; focused
  tests, isolated real-tmux tests, dependency/license review, canonical
  `./scripts/verify.sh`, docs, rollback proof, and the C022 Change Review pass.
  - Evidence: pending

## Authority Envelope

### May Continue Without Asking

- Approved local, reversible T0/T1 implementation, tests, documentation, source
  builds, branch commits, and isolated disposable tmux experiments required by
  C022.
- The bounded local T2 feasibility and implementation work explicitly described
  in the approved C022 proposal, provided it preserves current authentication,
  network exposure, PWA behavior, live tmux sessions, and production services.

### Must Pause for Approval

- Scope expansion, auth/origin/CSRF exceptions, caller-controlled commands,
  destructive or compatibility-breaking changes, production/live-session
  effects, Tailscale changes, secrets, external hardware/services, GitHub push
  or CI execution, publication, installer/signing work, merge, or any unapproved
  T2/T3 action.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Transport/auth spike | completed | Photino reaches a configured test server, completes protected auth, lists inventory, attaches xterm.js, and detaches without a security exception. | 13 URL tests, frontend typecheck/build, isolated protected Photino/tmux runtime passed |
| 2. Shell and profiles | in progress | Self-contained desktop shell and safe multi-profile settings handle launch, validation, auth, TLS, offline, and reconnect states. | 21 URL/profile tests, owner-only settings inspection, native chooser round trip; offline/reconnect remains |
| 3. Session thin slice | pending | One desktop tab creates one real tmux client and every clean/abrupt close path detaches only that client. | Server integration and isolated real-tmux lifecycle tests |
| 4. Tmux topology | pending | Session/window/pane tabs and splits use fixed typed operations and round-trip across reconnect/mobile. | Contract/security tests and isolated topology round trip |
| 5. Desktop interaction | pending | Keyboard/mouse terminal workflows, windows, create/kill, ordinary exit, and recovery meet AC5/AC6. | Frontend interaction suite and owner Ubuntu acceptance |
| 6. Cross-platform delivery | pending | Ubuntu/macOS source builds, launch evidence, docs, rollback, canonical gate, and review all pass. | `dotnet publish`, platform smoke tests, `./scripts/verify.sh`, Review Record |

Thin slice: complete Unit 3 so an Ubuntu desktop app can select a saved server,
authenticate, list sessions, attach one real tmux client, and close its tab while
the session remains running and becomes detached in the mobile PWA.

## Progress

- 2026-08-29: owner approved Photino/xterm.js, remote-server-only operation,
  configured Tailscale URL, tmux-authoritative session/window/pane mapping, real
  attachment state, detach-on-close, normal `exit`, explicit list-based kill,
  Apple Silicon, and source-build delivery.
- 2026-08-29: C022 product contract, proposal, durable decision, roadmap item,
  and resumable goal drafted; implementation has not begun.
- 2026-08-29: added the .NET 10/Photino shell, separate desktop React/xterm.js
  build, same-origin `/desktop/` route, initial session list/create/kill/tab UI,
  URL validation, and focused desktop tests.
- 2026-08-29: an isolated API-key server and tmux socket proved protected login,
  inventory rendering, one real tmux client on tab open, audited disconnect on
  tab close, and survival of the detached session. Unit 1 is complete.
- 2026-08-29: added a native multi-server chooser with atomic owner-only profile
  storage, strict URL/label validation, add/edit/delete/connect operations, and
  a desktop-only return control. An isolated Photino run saved a profile,
  connected to the remote desktop, and returned to the chooser without storing
  a login key or terminal content. Unit 2 remains open for offline/reconnect.

## Evidence

- Product approval: owner accepted every recommended first-cut behavior in the
  2026-08-29 design discussion.
- Planning validation: `git diff --check` passed; all required goal sections are
  present; `rg -l '^Status: active$' GOALS --glob '*.md'` reports only this goal.
- Canonical attempt: `./scripts/verify.sh` passed the first-run setup,
  healthcheck-watchdog, and workspace-recovery shell suites, then exited 145 at
  `dotnet restore` because this environment has SDK 8.0.130 while `global.json`
  requires 10.0.300. No product test failed and no toolchain change was made.
- Desktop URL tests: .NET 10 SDK container ran 13/13 passing tests for HTTPS,
  loopback-only development HTTP, credential/path/query rejection, and canonical
  `/desktop/` construction.
- Desktop frontend: `npm --prefix src/TmuxMobile.Web run typecheck` passed and a
  production Vite build emitted the independent desktop HTML/CSS/JS graph.
- Isolated runtime: local image `sha256:6f7055d...` served a CSP-restricted
  `/desktop/` page; Photino rendered it under Xvfb. API-key login audited one
  failed synthetic-input attempt and one successful owner login without key
  disclosure. Opening `protected-spike` produced one tmux client and a successful
  `terminal.connect`; closing the tab produced `terminal.disconnect`, returned
  `session_attached=0`, and left the session alive. The disposable container,
  tmux socket, Photino process, and X server were then removed/stopped.
- Canonical gate: with the ignored local .NET 10 SDK cache, `./scripts/verify.sh`
  exits 0 after the profile slice with 27 Core, 23 Infrastructure (four
  expected opt-in skips), 48 Server integration, 21 Desktop, and five frontend
  suites passing, followed by both Compose positive and fail-closed
  configuration assertions.
- Native profile runtime: `/tmp/tmuxctl-c022-profiles.pau3K9/profiles.json` was
  mode `0600` inside a mode-`0700` directory and contained only version, ID,
  label, and server URL. Photino connected profile `local` to the disposable
  server and its Servers control returned to the chooser. The app, X server,
  container, and temporary settings were then removed.

## Discoveries

- Existing secure-cookie, CSRF, origin, and WebSocket assumptions are designed
  for a same-origin PWA. Photino remote-auth topology must be proven before the
  desktop asset-serving architecture is selected.
- Existing PTY attachment already provides the correct basic client ownership
  boundary, but bounded abrupt-loss cleanup and tmux topology operations need
  explicit end-to-end evidence.
- ASP.NET endpoint matching treats `/desktop` and `/desktop/` equivalently; an
  explicit redirect caused a loop and was removed. Static default-file handling
  serves `/desktop/` directly.
- The canonical Compose assertion still expected the older equals-sign
  `extra_hosts` spelling while the approved file uses YAML's colon spelling;
  the verifier now accepts either exact separator without relaxing the required
  `host.docker.internal` to `host-gateway` mapping.
- A server-hosted desktop entry point preserves every existing same-origin
  control and avoids a native token/cookie bridge. Photino needs only the
  validated server origin and never receives the login key through native code.
- The remote desktop needs one narrow native bridge operation to reopen the
  server chooser. Profile mutations are valid only while the native chooser is
  displayed, keeping remote page content outside the settings-write boundary.

## Decisions

- Use .NET 10/Photino and xterm.js; do not build or embed a native terminal in
  this cut.
- Keep tmux authoritative: desktop session tab = tmux session, subordinate tab
  = tmux window, split = tmux pane.
- Close detaches app-owned clients; explicit named confirmation kills a session;
  terminal input is never interpreted as an app lifecycle command.
- Target Ubuntu x64 and Apple Silicon macOS source builds before installers or
  release distribution.
- Serve versioned desktop assets from the remote tmuxctl server at `/desktop/`;
  keep the Photino shell small and retain Strict same-origin security semantics.
- Persist only versioned profile IDs, labels, and normalized origins in a
  device-local atomic JSON file with Unix mode `0600` under a mode-`0700`
  directory; keep authentication entirely in the server-hosted page.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- Complete Unit 2 with explicit offline/reconnect states, then prove Unit 3's
  clean and abrupt desktop-close paths detach only the app-owned tmux client
  within a documented bound while leaving the session alive.

## Pause Conditions

- Pause if the spike requires weakening secure cookies, CSRF, origin/Host
  validation, authorization, rate limits, or network exposure.
- Pause if a new dependency cannot satisfy licensing or Ubuntu/macOS runtime
  requirements, or if Photino cannot provide required WebSocket/input behavior.
- Pause before using live sessions, production services, remote CI/Mac hardware,
  pushing/publishing, merging, or expanding to deferred packaging/platform work.
- After two unchanged failures, stop and record evidence rather than changing
  architecture without approval.

## Outcomes

- Unit 1 complete: the selected server-hosted desktop architecture preserves
  protected same-origin transport and proves real tmux attach/detach behavior in
  an isolated Photino runtime.
- Unit 2 profile slice complete: native profile management and its security
  boundary are implemented and runtime-proven; resilience remains active before
  the unit can close. Units 2-6 remain active.
