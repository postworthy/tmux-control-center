# Goal: tmuxctl Photino Desktop Companion

Status: paused
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
| 2. Shell and profiles | completed | Self-contained desktop shell and safe multi-profile settings handle launch, validation, auth, TLS, offline, and reconnect states. | 21 URL/profile tests, owner-only settings inspection, native chooser and offline/reconnect runtime |
| 3. Session thin slice | completed | Each open desktop tab creates one real tmux client and clean/abrupt close paths detach only app-owned clients. | Server integration plus isolated two-session, clean-close, network-loss, reconnect, and abrupt-close runtime |
| 4. Tmux topology | completed | Session/window/pane tabs and splits use fixed typed operations and round-trip through authoritative tmux state. | Core/parser/fixed-argv tests, 50 server integration tests, opt-in isolated topology test, Photino/tmux UI round trip |
| 5. Desktop interaction | completed | Keyboard/mouse terminal workflows, windows, create/kill, ordinary exit, and recovery meet AC5/AC6. | 26 desktop tests, six frontend suites, isolated keyboard/pop-out/exit/named-kill runtime; owner acceptance remains at the goal boundary |
| 6. Cross-platform delivery | in progress | Ubuntu/macOS source builds, launch evidence, docs, rollback, canonical gate, and review all pass. | `dotnet publish`, platform smoke tests, `./scripts/verify.sh`, Review Record |

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
  a login key or terminal content.
- 2026-08-29: kept every open session tab mounted, added inventory/terminal
  reconnect with capped exponential backoff, handled terminal heartbeats as
  control messages, and added clean page/tab close paths. An isolated two-
  session run proved tab switching preserves both attachments, tab close
  detaches only its client, network loss detaches while sessions survive,
  reconnect creates one fresh client, and abrupt Photino loss cleans up the
  final client. Units 2 and 3 are complete.
- 2026-08-29: added opaque window IDs, authoritative session topology, and a
  closed set of create/select/split/resize/close operations with strict JSON,
  CSRF, authorization, rate limiting, audit, stale-target rejection, and atomic
  final-window/final-pane protection. The desktop maps these to subordinate
  window tabs and pane controls. Isolated tmux and Photino runs proved all
  operations, including corrected resize ordering and guarded close. Unit 4 is
  complete.
- 2026-08-29: added capture-phase desktop session shortcuts, guarded native
  clipboard copy/paste, native session pop-outs, strict pop-out target
  validation, per-window native bridge state, exact-name session kill, and
  inventory-driven stale-tab pruning. An isolated Ubuntu/Photino runtime proved
  two independent attachments, tab cycling, detach-only tab close, ordinary
  terminal `exit`, native child-window close, and wrong/exact kill confirmation.
  Unit 5 is complete pending final owner acceptance at the goal boundary.
- 2026-08-29: resolved the review's older-server compatibility finding with a
  shared version-1 capability contract, an anonymous content-free rate-limited
  endpoint, and a bounded native HTTPS preflight that refuses redirects and
  remote UI until the closed feature set is present. Missing support now
  returns to the native chooser with an explicit server-update message.

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
  exits 0 after Unit 4 with 27 Core, 26 Infrastructure (five expected opt-in
  skips), 50 Server integration, 21 Desktop, and six frontend suites passing,
  followed by both Compose positive and fail-closed configuration assertions.
- Native profile runtime: `/tmp/tmuxctl-c022-profiles.pau3K9/profiles.json` was
  mode `0600` inside a mode-`0700` directory and contained only version, ID,
  label, and server URL. Photino connected profile `local` to the disposable
  server and its Servers control returned to the chooser. The app, X server,
  container, and temporary settings were then removed.
- Desktop lifecycle runtime: disposable image `sha256:eee9e208...` and socket
  `c022_lifecycle` hosted sessions `alpha` and `beta`. Opening both produced
  client PIDs 103 and 105; selecting alpha left both clients attached; closing
  alpha left only beta and `alpha:0`. Disconnecting the disposable container's
  bridge produced `alpha:0` and `beta:0` plus a visible server-offline state.
  Reconnecting restored beta as one new client (PID 200). Sending SIGQUIT to
  Photino then removed that client immediately, the disconnect was audited, and
  both tmux sessions remained alive. All disposable processes and state were
  removed afterward.
- Frontend resilience checks: TypeScript compilation passes and the sixth unit
  suite proves 1-30 second bounded reconnect delays plus terminal heartbeat
  discrimination.
- Initial connection failure runtime: Photino navigated to unused loopback port
  55999, then its native 12-second watchdog returned to the chooser with a
  server/network/TLS error and a usable profile form. The disposable app and X
  server were stopped and no settings were created.
- Topology tests: 3 fixed-argv/parser tests and 2 server contract/security tests
  cover opaque IDs, strict request fields, CSRF, stale targets, bounded enums,
  auditing, and atomic final-close conflicts. The opt-in real tmux topology test
  creates/selects windows, splits/selects/resizes/closes panes, closes a
  non-final window, rejects both final close paths, and confirms session
  survival.
- Topology runtime: disposable images `sha256:d28959ed...` and corrected
  `sha256:d144a06c...` served socket-isolated tmux 3.4. Photino rendered window
  tabs and pane chips, created and selected `editor`, split it left/right and
  top/bottom, selected and closed a pane, closed a non-final window, and visibly
  refused final window/pane close while `has-session` remained successful. The
  corrected resize control changed the active pane from 62 to 64 columns and
  the sibling from 61 to 59, matching direct tmux inventory. Disposable
  containers, sockets, Photino, Xvfb, and input harness files were removed.
- Desktop interaction tests: 26/26 native tests validate URL/profile behavior,
  strict native commands, and opaque session pop-out targets; frontend
  typecheck and all six suites pass, including bounded input serialization and
  reconnect behavior.
- Desktop interaction runtime: disposable image `sha256:f9a39844...` and
  socket `c022_interaction` hosted `alpha`, `beta`, and `gamma`. Opening alpha
  and beta produced distinct PTY clients 206 and 211. Ctrl+PageUp changed the
  active xterm tab while both stayed attached; Ctrl+Shift+W removed only client
  206 and left `alpha:0` and `beta:1`. Typing ordinary shell `exit` removed beta
  and the authoritative inventory pruned its tab without retrying a stale
  target. A wrong typed kill confirmation preserved gamma; exact `gamma`
  removed it. Earlier in the same interaction checkpoint, the native pop-out
  created a second 1280x800 Photino window and tmux client, and closing that
  child left the root window and its attachment intact.
- Source delivery: `scripts/build-desktop.sh` produced current self-contained
  outputs for `linux-x64` (80 MiB) and `osx-arm64` (84 MiB). `file` identified
  the launchers as x86-64 ELF and arm64 Mach-O respectively, the macOS Photino
  library includes arm64, and Linux native linkage reported no missing
  libraries. The Linux artifact launched under Xvfb, rendered live inventory,
  and stayed usable with `DOTNET_ROOT=/nonexistent` and multilevel lookup off.
  Actual Apple Silicon launch remains the external hardware/CI boundary.
- Dependency review: `dotnet list TmuxMobile.sln package --vulnerable
  --include-transitive` reports no vulnerable direct or transitive NuGet
  packages in all eight projects; `npm audit --omit=dev` reports zero
  vulnerabilities. `THIRD_PARTY_NOTICES.md` records Photino.NET 4.0.16 and its
  Apache-2.0 license.
- Pre-capability canonical gate: `./scripts/verify.sh` exited 0 with shell recovery and
  watchdog suites, 27 Core, 26 Infrastructure plus five intentional opt-in
  skips, 50 Server integration, 26 Desktop, six frontend suites, and both
  Compose configuration boundaries passing.
- Rollback boundary: the C022 range begins after `24077b6`; a path-restricted
  diff confirms existing mobile source and workspace-recovery source are
  unchanged. Reverting the C022 commits removes the additive desktop/API
  surface, after which `./scripts/verify.sh` is the validation gate.
- Change Review: `REVIEWS/2026-08-29--photino-desktop-companion.md` records a
  `not ready` decision only because actual Apple Silicon launch and owner Ubuntu
  interaction acceptance remain. The predecessor-stack boundary and required
  commit trailers are resolved; the earlier compatibility finding is resolved
  by rewritten commit `8741bf4`.
- Capability tests and runtime: 8 new native cases cover the exact endpoint,
  forward-compatible versions, missing features, old-server 404/401, redirect
  refusal, malformed/oversized bodies, and sanitized TLS failure. The anonymous
  server contract test verifies exact content-free fields and cache prevention.
  A real Photino run against a 404-only loopback server requested
  `/api/desktop/capabilities`, did not load remote UI, and displayed “Update the
  tmuxctl server”; a second run against isolated image `sha256:c890fe22...`
  accepted protocol 1, loaded `/desktop/`, and established the inventory
  WebSocket.
- Post-capability source delivery and canonical gate: current self-contained
  `linux-x64` and `osx-arm64` outputs again build as x86-64 ELF and arm64 Mach-O
  artifacts with the shared protocol assembly. `./scripts/verify.sh` exits 0
  with 27 Core, 26 Infrastructure plus five intentional skips, 51 Server
  integration, 34 Desktop, six frontend suites, shell suites, and Compose
  boundary assertions passing.
- Invalid-TLS runtime: a real Photino launch probed a disposable self-signed
  HTTPS endpoint on loopback. Operating-system certificate validation rejected
  it before remote navigation and the native chooser displayed “The server TLS
  certificate could not be verified” with certificate/URL guidance. The app,
  TLS server, certificate material, and dedicated X server were then stopped or
  removed.
- Physical acceptance handoff: `docs/desktop-acceptance.md` records clean-build,
  protocol, Ubuntu, Apple Silicon, profiles, attachment, topology, clipboard,
  context-menu, resize, network, sleep/wake, exit/kill, PWA-regression, privacy,
  and sanitized-report steps without asking the owner to disclose secrets or
  terminal content.
- 2026-08-29: the owner selected predecessor-stack ordering and authorized local
  history repair. Local `main` now ends at the declared C022 baseline
  `24077b6`; the C022 merge base is scope-clean. Rewritten interaction commit
  `07b6d95` carries all required trailers, and replaying its descendants
  preserved the exact pre/post-rewrite HEAD tree hash
  `e5f790766707e06a8b4a4249a1175991d56f2859`.
- 2026-08-29: the unqualified post-repair canonical attempt exited 145 after
  its shell suites because the host default is SDK 8 and the repository pins
  .NET 10. Re-running through the existing ignored repo-local SDK 10.0.302
  exits 0: 27 Core, 26 Infrastructure plus five intentional skips, 34 Desktop,
  51 Server integration, all six frontend suites, the shell suites, and both
  Compose assertions pass. No toolchain contract changed. The goal is paused
  only for physical Ubuntu and Apple Silicon acceptance. The optional
  `pnpm goal:status` adapter is unavailable because this repository has no root
  package manifest; direct status inspection confirms C022 is paused and no
  goal is marked active.
- 2026-08-29: the first physical Ubuntu connection attempt stopped in the
  native capability preflight with its sanitized generic connection error.
  `RCA/2026-08-29--desktop-magicdns-preflight-failure.md` proves the host has
  Tailscale DNS acceptance disabled, so its own MagicDNS origin does not resolve
  normally even though forcing the hostname to the current Tailscale IP reaches
  Kestrel through valid TLS. That forced request also proves the deployed image
  predates C022 because the capability path returns mobile HTML rather than
  protocol JSON. No network or deployment state was changed.
- 2026-08-29: the owner enabled Tailscale DNS acceptance and explicitly
  authorized C022 deployment. The prior image `sha256:f48be26d...` is preserved
  as `tmux-mobile:pre-c022-desktop-rollback-20260829`; the new
  `sha256:8873036d...` image passed Compose validation and the isolated tmux 3.4
  socket probe before replacement. It is healthy with zero restarts on the
  unchanged loopback/Serve boundary. HTTPS liveness is 200, direct backend
  application access remains 426, and the live capability endpoint returns the
  exact protocol-1 feature contract. Mobile and desktop entry points both
  render their distinct asset graphs, bounded logs show no failure, and six
  host tmux sessions are present after deployment.

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
- An error document from an unreachable or TLS-invalid origin cannot render the
  remote Servers control. A native navigation watchdog must require a
  non-secret desktop-ready message and return to the chooser on timeout.
- Rendering only the selected terminal is not equivalent to keeping a desktop
  tab open: React unmount would detach the inactive tmux client. All open tab
  terminals must remain mounted and only their presentation may be hidden.
- Tmux `resize-pane` accepts its numeric adjustment after the target options;
  placing it before `-t` is parsed as an extra positional argument and fails.
  The fixed service vector and regression test now encode the proven order.
- Server-hosted desktop assets make same-origin security simple, but an older
  server cannot render the compatibility UI at all. The native shell therefore
  needs an explicit pre-navigation capability check and actionable version
  error to meet the approved compatibility note; a generic navigation timeout
  is insufficient.
- The compatibility gap is resolved without loading or trusting older remote
  content: native `HttpClient` uses the operating-system TLS validation,
  disables redirects, bounds the response to 16 KiB, accepts additive future
  metadata, and requires all version-1 capabilities before WebView navigation.
- The owner-approved predecessor-stack fast-forward makes local `main`, the
  C022 merge base, and the declared baseline all resolve to `24077b6`; only C022
  commits remain in `main..HEAD`.
- A same-host Tailscale Serve URL still depends on operating-system MagicDNS
  resolution. `tailscale status` and a forced-address HTTPS request do not prove
  that the native app can resolve the saved hostname; physical preflight must
  check Tailscale DNS acceptance and a normal resolver lookup separately.

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
- Retry inventory and terminal sockets with 1-30 second exponential backoff;
  treat server heartbeat JSON as control data, never terminal output.
- Use opaque `w_`/`p_` targets and fixed typed topology actions. Refuse an
  operation that would close the final session-bearing window/pane atomically
  within tmux rather than relying on a racy read-then-write check.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure resolved: the first physical Ubuntu compatibility attempt could
  not resolve the MagicDNS origin because Tailscale DNS acceptance was disabled. A
  forced-address request then showed the deployed server is also pre-C022. The
  supported causes and verification gap are recorded in the related RCA.
  Tailscale DNS acceptance is now enabled and the C022 server is live, so the
  next physical attempt changes both proven causes.
- Resume evidence: the owner chose predecessor-stack ordering and authorized
  local history repair. Both Git findings are resolved with an unchanged source
  tree and a passing canonical gate.
- Remaining boundary: physical Ubuntu interaction and actual Apple Silicon
  launch evidence described in `docs/desktop-acceptance.md` cannot be produced
  by the current headless Linux execution environment.

## Next Action

- Have the owner reconnect the Ubuntu desktop client to the unchanged `:8443`
  origin and continue the numbered physical acceptance checklist.

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
- Units 2 and 3 complete: native profiles, offline/reconnect behavior, stable
  multi-tab attachments, clean detach, reconnect, and abrupt-loss cleanup are
  implemented and runtime-proven.
- Unit 4 complete: authoritative tmux windows/panes and their guarded typed
  operations are automated- and runtime-proven. Units 5-6 remain active.
- Unit 5 implementation complete: conventional desktop shortcuts, independent
  windows, guarded clipboard paths, exact-name kill, ordinary `exit`, and
  authoritative tab cleanup are implemented and locally runtime-proven. Unit 6
  and physical owner/platform acceptance remain active.
- Unit 6 is paused only at owner/external boundaries: local source builds,
  Linux launch, compatibility, TLS, tests, docs, rollback, and review evidence
  are complete; branch ordering and commit metadata are resolved, while Apple
  Silicon launch and owner Ubuntu acceptance remain required.
