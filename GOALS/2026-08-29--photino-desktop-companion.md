# Goal: tmuxctl Photino Desktop Companion

Status: in progress
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-31
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

- [ ] AC1 — Clean-checkout documented commands produce a self-contained
  `linux-x64` executable/Ubuntu launcher and an `osx-arm64` `.app`, both using
  the PWA artwork for native pinning; Ubuntu launches without installed .NET and
  actual Apple Silicon hardware or approved macOS CI proves the macOS bundle
  launches and reaches its connection screen.
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
- [ ] AC4 — Dragging a desktop session tab shows exactly one global set of five
  labeled snap targets, edge drops create nested left/right or top/bottom
  layouts, and center or **Single view** restores one standard group; every
  session appears once with one attachment and tmux windows/panes remain
  authoritative without subordinate topology chrome.
  - Evidence: pending
- [ ] AC5 — Session listing, selection, creation, validated rename, detach, and
  two-click confirmed kill work only on inventory-resolved targets; rename
  updates the sidebar and every open tab without reconnecting, closing UI never
  kills a session, and typed `exit` retains ordinary tmux pane/window/session
  behavior.
  - Evidence: pending
- [ ] AC6 — Desktop keyboard navigation, independent windows, focus, selection,
  copy/paste, one tmux-owned right-click menu without a duplicate tmuxctl
  overlay, coalesced unmodified-wheel tmux history by default, a device-local
  per-session App Scroll toggle for foreground tools, reliable
  initial/maximized/fullscreen fitting, resize, bounded Ctrl+mouse-wheel text
  zoom, collapsible icon-rail navigation, draggable split groups, and reconnect
  pass automated interaction checks and owner Ubuntu acceptance without mobile
  cards, swipe navigation, touch shortcuts, or oversized mobile controls.
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
| 5. Desktop interaction | in progress | Keyboard/mouse terminal workflows, settled initial/fullscreen fit, coalesced tmux-history wheel input with explicit per-session App Scroll routing, bounded Ctrl+wheel text zoom, one global five-zone split overlay, single-view reset, collapsible sidebar, working pop-outs, create/rename/kill, ordinary exit, and recovery meet AC4-AC6. | Focused layout/wheel/pop-out tests, frontend suites, physical Ubuntu acceptance, and canonical verification |
| 6. Cross-platform delivery | in progress | Ubuntu executable/launcher and macOS app-bundle source builds carry the PWA icon; launch/pinning evidence, docs, rollback, canonical gate, and review all pass. | `dotnet publish`, platform smoke tests, bundle/launcher checks, `./scripts/verify.sh`, Review Record |

Thin slice: complete Unit 3 so an Ubuntu desktop app can select a saved server,
authenticate, list sessions, attach one real tmux client, and close its tab while
the session remains running and becomes detached in the mobile PWA.

## Progress

- 2026-08-30: mobile PWA feedback showed session inventory remained available
  while every terminal attachment fell into Reconnect whenever two desktop tabs
  were open. RCA proved the unchanged per-owner terminal lease limit of two was
  fully consumed by the desktop's persistent attachments, and the silent 429
  rejection preceded terminal audit and PTY startup. The bounded local
  correction raises global/per-owner defaults to ten, adds rejection evidence,
  and adds a capacity/release regression test. The focused test and canonical
  verification pass; deployment remains at the owner boundary.
- 2026-08-30: the owner authorized the capacity correction build and deployment.
  The prior live image `sha256:0370d1bc...` is preserved as
  `tmux-mobile:pre-terminal-capacity-20260830`. Corrected image
  `sha256:1fbc3c0a...` passed Compose validation and the isolated host/container
  tmux 3.4 socket probe, then replaced only the app service. It is healthy with
  zero restarts, publishes only on host loopback, reports the bounded 10/10
  capacity, serves HTTPS root/liveness/protocol metadata, and retains direct
  backend denial. All seven sessions and windows survived unchanged. With both
  desktop clients reconnected, a third owner terminal attached successfully and
  sent mobile history controls; the affected session's attachment count changed
  from two to three, proving the previously rejected desktop-plus-mobile path.
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
- 2026-08-30: physical Ubuntu feedback showed continued native-window resize
  was not delivered through the browser-only fit triggers. Added bounded host-
  geometry detection, terminal right-click horizontal/vertical split actions,
  and per-session collapsed-rail icons, then deployed the corrected server and
  rebuilt the self-contained Ubuntu launcher.
- 2026-08-30: physical Ubuntu feedback showed post-capability native pop-outs
  stuck on their progress document and requested VS Code-style session editor
  groups. RCA traced the pop-out regression to asynchronous navigation inside a
  blocking child-window loop. The local correction deep-links known-compatible
  child windows and adds draggable nested left/right and top/bottom groups with
  visible snap guidance, unique session membership, and empty-group collapse.

## Evidence

- Mobile capacity RCA: the live container had two server-owned `tmux
  attach-session` children while effective per-owner capacity was two. The
  owner's `2026-08-30T14:11:54Z` login and inventory connection succeeded, but
  no terminal attempt reached audit because the lease-rejection branch returned
  429 first. `TerminalConnectionLimiterTests` passes and proves ten bounded
  same-owner leases, rejection of the eleventh, and lease reuse after disposal.
  The canonical `./scripts/verify.sh` exits 0 with 27 Core, 26 Infrastructure
  plus five intentional skips, 56 Server integration, 41 Desktop, twelve
  frontend suites, shell suites, and Compose assertions passing.
- Mobile capacity deployment: Compose image
  `tmux-mobile:terminal-capacity-b655763` resolves to
  `sha256:1fbc3c0ade52cb57d0ea5fcd845ab6ebf89cd9e7c2be03bfddaf7770cb59e5a6`;
  rollback tag `tmux-mobile:pre-terminal-capacity-20260830` resolves to prior
  digest `sha256:0370d1bc8a0bb1dc9043c7a39d0a6081835057ede9c34c902395c3ecfc28558d`.
  `scripts/first-run-setup.sh probe-tmux` passed on an isolated tmux 3.4 socket.
  Live HTTPS root, liveness, and desktop capabilities returned 200; the backend
  returned 426 with the configured Host; Docker exposes only
  `127.0.0.1:8780`; health is `healthy` with restart count zero. Pre/post tmux
  inventory retains the same seven session IDs, names, and window counts. Live
  server process evidence shows three owner PTY clients, and audit/log evidence
  records the third successful terminal connection plus mobile history actions
  while the two persistent desktop clients remain present.
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
- Desktop interaction tests: 34/34 native tests validate URL/profile behavior,
  strict native commands, and opaque session pop-out targets; frontend
  typecheck and all nine suites pass, including bounded input serialization,
  layout/history correction, and reconnect behavior.
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
- Desktop font zoom: pure modifier/range cases prove Ctrl+wheel-up/down changes
  one point, unmodified wheel bypasses zoom, invalid state recovers safely, and
  8px/32px bounds hold. TypeScript compilation and the production desktop bundle
  pass; physical wheel behavior remains in owner acceptance.
- Desktop corrective interaction: unmodified wheel input is now coalesced into
  typed authoritative tmux-history requests no faster than the server's
  four-operation-per-second limit. Xterm fitting waits for measurable geometry,
  retries after WebKit layout settles, observes both host and stage, and responds
  to window, fullscreen, and visibility transitions. The primary surface now
  has one session-tab row and a 48px collapsed sidebar icon rail. Two new focused
  suites pass alongside all nine frontend suites; the production desktop build
  and canonical repository gate pass. Physical behavior remains owner acceptance.
- Desktop native-resize correction: the active terminal now compares its
  measurable host geometry every 100 ms in addition to DOM observers, so a
  Photino/WebKitGTK window-size change cannot be missed. Pure geometry and
  navigation tests bring the frontend total to ten passing suites. The terminal
  context menu resolves the authoritative active tmux pane before issuing a
  typed split, and the collapsed rail lists every session with initials and
  attachment state. Canonical verification passes with 27 Core, 26
  Infrastructure plus five intentional skips, 55 Server integration, 35
  Desktop, and ten frontend suites. Live image `sha256:54ce005...` is healthy
  with zero restarts, serves bundle `index-BuJxit0H.js`, and preserves image
  `sha256:ba16379...` as `tmux-mobile:pre-desktop-native-resize-20260830`.
- Desktop group/pop-out correction: four native navigation cases prove pop-outs
  retain strict opaque targets and receive a cache-busted session deep link
  without a second progress-state negotiation. Pure workspace tests prove
  side-by-side and stacked nesting, center moves, all five snap zones, unique
  membership, stale-session pruning, and empty-group collapse. Canonical
  verification passes with 39 Desktop, 55 Server integration, 27 Core, 26
  Infrastructure plus five intentional skips, and eleven frontend suites. The
  self-contained Ubuntu launcher builds locally; native interaction and live
  deployment remain at the owner boundary.
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
- 2026-08-29: during physical Ubuntu acceptance, the owner required conventional
  Ctrl+mouse-wheel terminal text zoom. The desktop-only xterm host now captures
  that modified wheel gesture before browser/xterm scrolling, adjusts one point
  per event within an 8–32px bound, refits the active terminal, and reports the
  resulting dimensions to tmux. Unmodified wheel events bypass the zoom path,
  and the mobile terminal remains unchanged. Focused typecheck, the production
  desktop build, all seven frontend suites including the new modifier/bounds
  cases, and the canonical repository gate pass. The change is local and awaits
  a separately approved live redeployment plus owner interaction acceptance.
- 2026-08-29: continued physical acceptance found that unmodified wheel input
  does not navigate tmux history, initial and fullscreen xterm fitting depend on
  a later manual window resize, the permanent session/window/pane hierarchy is
  too dense, and the sidebar cannot collapse. The owner explicitly superseded
  the three-row presentation with one session-tab row and requested a VS Code-
  style icon rail. `RCA/2026-08-29--desktop-layout-and-history-acceptance.md`
  traces the fit lifecycle, wheel route, prior contract mismatch, and missing
  physical verification before corrective implementation.
- 2026-08-29: the owner explicitly authorized complete rebuild and deployment
  of the correction. Image `sha256:d6dadb4f...` built from commit `99b2af7`,
  passed the isolated host/container tmux 3.4 socket probe, and replaced only
  the Compose app service. It is healthy with zero restarts on the unchanged
  loopback `127.0.0.1:8780` and Tailscale Serve `:8443` boundary. HTTPS liveness
  is 200, the capability contract is exact protocol 1, the live desktop entry
  references corrected bundle `index-DcoKvsuW.js`, and direct backend access
  remains 426. Prior image `sha256:8873036...` is preserved as
  `tmux-mobile:pre-desktop-layout-rollback-20260829`. A fresh self-contained
  Ubuntu x64 launcher build completed with no missing linked libraries.
- 2026-08-29: owner acceptance immediately rejected that rollout because the
  running desktop still presented all three pre-correction behaviors. Live and
  in-image asset digests match the corrected bundle, while the actual
  `/desktop/` default-document response lacks cache-control headers and native
  navigation reuses the same URI with a persistent WebKit cache. RCA
  `RCA/2026-08-29--desktop-webview-stale-release.md` records the supported stale-
  release cause and narrows the next attempt to explicit document no-store plus
  native cache-busted navigation. No corrective redeployment has occurred yet.
- 2026-08-29: the narrowed cache-boundary correction is implemented. Native
  navigation now adds a unique non-secret `desktopLoad` token while preserving
  encoded session deep links; every non-asset `/desktop` document/fallback path
  now emits `no-store, no-cache` plus `Pragma: no-cache`. All 35 native desktop
  tests, five focused server cases, 55 total server integration tests, and the
  canonical gate pass. Rebuild/probe/redeployment remain next.
- 2026-08-29: self-contained Ubuntu launcher and image `sha256:ba16379d...`
  rebuilt from corrective commit `021b8a8`. The isolated tmux 3.4 probe passed;
  the image replaced only the Compose app service and is healthy with zero
  restarts. The actual live cache-busted desktop document now returns
  `no-store, no-cache` plus `Pragma: no-cache`; HTTPS liveness, protocol 1, and
  loopback-only binding remain intact. Immediate rollback is preserved as
  `tmux-mobile:pre-webview-cache-rollback-20260829`. Owner interaction remains
  required before accepting the three reported UI behaviors.
- 2026-08-30: the owner authorized deployment of the pop-out/session-group
  correction committed as `2044254`. The prior live image
  `sha256:54ce0059...` is preserved as
  `tmux-mobile:pre-desktop-session-groups-20260830`; replacement image
  `sha256:8be95175...` passed Compose validation and the isolated host/container
  tmux 3.4 socket probe before replacing only the app service. The container is
  healthy with zero restarts on the unchanged loopback/Serve boundary. The
  cache-busted HTTPS desktop document returns 200 with `no-store, no-cache`,
  references `index-CZwtEXak.js`, and the served JS/CSS contain the new
  drag/drop session-group markers. Protocol compatibility returns 200, direct
  backend HTTP remains 426, logs contain no errors, and all six named sessions
  retain their pre-deployment window and attachment counts. The self-contained
  Ubuntu x64 launcher is present and has all native library dependencies.
- 2026-08-30: owner feedback added native PWA-derived app identity for Ubuntu
  and macOS and reported that the first unsplit session group remains
  content-sized. RCA `RCA/2026-08-30--desktop-root-session-group-shrinks.md`
  proves the root group lacked the flex sizing applied to nested groups. The
  corrective root selector, Ubuntu window icon/launcher, launcher registration
  helper, and Apple Silicon `.app`/ICNS packaging are implemented locally. The
  Linux PNG is byte-identical to the PWA 512px asset; the macOS bundle contains
  an arm64 Mach-O executable and valid multi-size ICNS. An isolated launcher
  install passes, Photino logs the absolute `SetIconFile` call, actual GTK
  `WM_CLASS` is `Tmuxctl` as declared by the launcher, both source builds pass,
  and the canonical gate passes 41 Desktop, 55 Server integration, 27 Core, 26
  Infrastructure plus five intentional skips, and eleven frontend suites. No
  server redeployment or user launcher installation has occurred.
- 2026-08-30: owner screenshots from the rebuilt native launcher continued to
  show the narrow first root group. RCA
  `RCA/2026-08-30--desktop-root-layout-tested-before-redeployment.md` proves the
  selected server still serves pre-`e179c83` image `sha256:8be95175...` and
  stylesheet `index-BgbL6qd-.css`, whose live bytes lack the corrected direct-
  root flex selector. Because Photino loads `/desktop/` from the server, the
  native rebuild could expose the new icon but not the pending CSS correction.
  No new source correction or deployment was performed; the next attempt must
  verify the live stylesheet after explicitly approved replacement and before
  physical retest.
- 2026-08-30: the owner explicitly authorized the pending server deployment and
  Ubuntu launcher installation. Prior image `sha256:8be95175...` is preserved
  as `tmux-mobile:pre-root-layout-rollback-20260830`. Corrected image
  `sha256:a2d7f313...` passed Compose validation, in-image selector inspection,
  and the isolated tmux 3.4 probe before replacing only the app service. It is
  healthy with zero restarts; HTTPS protocol metadata remains 200, direct HTTP
  remains 426, bounded logs contain no error, and all six session/window/
  attachment counts match the immediate predeployment snapshot. The live
  cache-busted document references `index-KHTt-Jmq.css`, whose served bytes
  contain the required direct-root selector. The mode-`0644` Ubuntu launcher is
  installed under the user's application data, passes desktop-file validation,
  declares the observed `Tmuxctl` GTK class, and resolves to the executable plus
  a PNG byte-identical to the PWA icon.
- 2026-08-30: owner physical feedback rejected group-local snap guidance because
  each split multiplied the five targets and there was no obvious return to the
  standard layout. RCA `RCA/2026-08-30--desktop-snap-zones-multiply-per-group.md`
  proves `renderGroup` intentionally emitted five zones per leaf and the tests
  encoded group-local targeting without a zone-count or flatten guard. The
  approved interaction contract now requires one global labeled five-zone
  overlay, root-relative edge splits, and center/sidebar **Single view** reset
  while preserving unique membership and attachments. The narrow correction is
  implemented locally; focused typecheck, production bundle inspection, all
  eleven frontend suites, the desktop delivery guard, and canonical verification
  pass. The canonical gate reports 41 Desktop, 55 Server integration, 27 Core,
  26 Infrastructure plus five intentional skips, all shell suites, and Compose
  assertions. Redeployment remains next.
- 2026-08-31: the owner authorized deployment of the independently committed
  native-maximize refit (`2f76c73`) and reconnect-stability correction
  (`66c7a53`). The prior live image `sha256:82b787f4...` is preserved as
  `tmux-mobile:pre-desktop-stability-20260831`; replacement image
  `tmux-mobile:desktop-stability-66c7a53` has digest `sha256:696d49f9...`,
  passed the disposable host/container tmux 3.4 probe, and replaced only the
  Compose app service. It is healthy with zero restarts on the unchanged
  `127.0.0.1:8780` backend/Serve boundary. HTTPS liveness and exact protocol-1
  capabilities pass through ordinary MagicDNS, the cache-busted document is
  HTTP 200 with `no-store, no-cache` and references `index-6mG0icpN.js`, and
  the served bundle contains both native-geometry bridge markers. Bounded logs
  contain no failure or reconnect churn. All six predeployment tmux sessions
  retain their exact IDs, names, window counts, and attachment states. The
  self-contained Ubuntu x64 client was rebuilt in place, has no missing native
  libraries, and the validated installed launcher still resolves to that
  executable and the PWA-derived icon. Because a client process predating the
  rebuild remains open, the owner must fully quit and relaunch it before
  physically accepting title-bar maximize/restore and reconnect behavior.
- 2026-08-31: physical testing after that relaunch proved maximize and
  reconnect are causally linked through an undocumented terminal-size boundary.
  RCA `RCA/2026-08-31--desktop-fullscreen-width-exceeds-pty-bound.md` records
  that unrestricted xterm fitting crosses the Linux PTY adapter's private
  500-column maximum on the 5120-pixel display. A disposable live WebSocket
  probe remains open at 500×65 and closes at 501×65 with code 1007 and
  `Invalid terminal message`, exactly matching the browser behavior. The native
  bridge and reconnect backoff both operate as implemented; the missing shared
  dimension contract makes every maximized retry fail. No corrective source or
  deployment change has occurred yet.
- 2026-08-31: the owner approved a 2048×1024 bounded correction and its
  deployment. Core now owns the 10–2048 by 5–1024 PTY contract, the WebSocket
  validates it before adapter invocation, Linux initial and resize paths enforce
  it, and desktop xterm clamps its own logical grid to matching bounds before
  transmission. Focused typecheck, all twelve frontend suites, production web
  build, delivery guards, 32 Core, 41 Desktop, 58 Server integration, and 26
  Infrastructure tests pass; all six opt-in Linux tests also pass with tmux 3.4,
  including real 2048 acceptance and 2049 rejection. The canonical host command
  passes every shell guard before stopping at the known host SDK 8 versus pinned
  SDK 10.0.300 boundary; its .NET leg passes in the compiler-equipped .NET 10
  image, and the remaining frontend and Compose assertions pass directly.
  Production build, rollback snapshot, deployment, and live 2048/2049 proof are
  next.
- 2026-08-31: corrective commit `e43e5f5` is deployed as
  `tmux-mobile:high-resolution-grid-e43e5f5` with digest
  `sha256:73fab731...`. The prior healthy image `sha256:696d49f9...` is retained
  as `tmux-mobile:pre-high-resolution-grid-20260831`. The candidate passed the
  disposable host/container tmux 3.4 probe before replacing only the Compose app
  service. The replacement is healthy with zero restarts on the unchanged
  loopback/Serve boundary; HTTPS liveness, readiness, protocol-1 compatibility,
  cache-busted `index-CLT48frC.js`, direct-backend 426 denial, and bounded logs
  pass. A protected disposable live WebSocket/real-PTY probe remains connected
  at 2048×65 for 2000 ms and rejects 2049×65 explicitly with code 1007 and
  `Invalid terminal dimensions`. The probe session was removed, and all six
  predeployment user sessions retain their exact IDs, names, one window, and
  attached state. Physical maximized-ultrawide acceptance remains.
- 2026-08-31: added a desktop-only F12 shortcut on the native chooser,
  compatibility screen, and server-hosted desktop page. It sends one fixed
  native bridge command per key press, toggles each Photino window's operating-
  system fullscreen state independently, and emits the existing geometry event
  so mounted xterms perform their settled refit. Ordinary browser access does
  not capture F12. Focused frontend, native, delivery, production-build, and
  full .NET 10 checks pass; deployment and physical chromeless-fullscreen
  acceptance remain at the owner boundary.
- 2026-08-31: added an explicit App Scroll toggle to every expanded desktop
  session row. The default path retains coalesced authoritative tmux history;
  enabling one session leaves its unmodified wheel events to xterm and batches
  negotiated mouse reports for the foreground tool without changing Ctrl+wheel
  font zoom. The bounded device-local preference updates mounted terminals,
  persists across reloads, and syncs across native windows without affecting
  other sessions. Focused routing, persistence, typecheck, delivery, and
  production-bundle checks pass; deployment and physical mouse-aware-tool
  acceptance remain at the owner boundary.
- 2026-08-31: the owner authorized the App Scroll build and deployment.
  Candidate `tmux-mobile:desktop-app-scroll-581b1dd` with digest
  `sha256:77150a97...` passed Compose validation and the isolated host/container
  tmux 3.4 probe before replacing only the app service. The prior image
  `sha256:73fab731...` remains tagged as
  `tmux-mobile:pre-desktop-app-scroll-20260831`. The replacement is healthy with
  zero restarts, HTTPS serves cache-busted `index-C5fgyXkz.js` and
  `index-HeZ-p1xA.css` containing App Scroll, F12, and active-toggle markers,
  direct application traffic remains denied with 426, and Docker remains bound
  only to `127.0.0.1:8780`. All six predeployment tmux sessions retain their
  exact IDs, names, one-window counts, and attachment states; the already-open
  desktop terminal reconnected to the replacement.

## Discoveries

- Persistent desktop tabs and mobile terminals share the same authenticated
  owner identity and therefore the same per-user terminal lease bucket. Any
  desktop concurrency contract must reserve enough bounded capacity for mobile
  use and be verified with both clients attached concurrently.
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
- Photino child `WaitForClose()` can pump a child window from a parent callback,
  but an async capability continuation that later invokes into that nested
  lifecycle can leave the child on its intermediate page. A pop-out requested
  by an already-ready page must use that established compatibility result and
  provide its startup navigation before entering the child loop.

## Decisions

- Use .NET 10/Photino and xterm.js; do not build or embed a native terminal in
  this cut.
- Keep tmux authoritative: a desktop tab maps to a tmux session. Tmux windows
  and panes remain available through ordinary tmux interaction inside xterm;
  retain their typed API for a future compact opt-in surface, not permanent rows.
- Close detaches app-owned clients; explicit two-click confirmation kills a session;
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

- Current attempt: 1 for the desktop App Scroll addition
- Maximum attempts per unchanged failure: 2
- Last observed failure: none for the App Scroll behavior. The live server now
  serves the new bundle; a window already open during replacement retains its
  in-memory prior JavaScript until the owner closes and relaunches it.
- Resume evidence: the owner chose predecessor-stack ordering and authorized
  local history repair. Both Git findings are resolved with an unchanged source
  tree and a passing canonical gate.
- Remaining boundary: physical Ubuntu interaction and actual Apple Silicon
  launch evidence described in `docs/desktop-acceptance.md` cannot be produced
  by the current headless Linux execution environment.

## Next Action

- Owner closes and relaunches the existing Ubuntu tmuxctl window, then confirms
  the expanded session row shows the App Scroll toggle and routes a mouse-aware
  foreground tool while disabled mode returns to tmux history. Rebuilding the
  pending F12-capable native launcher and Apple Silicon launch/pinning remain
  physical/external acceptance.

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
- Unit 5 implementation complete locally: conventional desktop shortcuts,
  independent windows, guarded clipboard paths, authoritative wheel history,
  settled initial/fullscreen fitting, bounded Ctrl+wheel font zoom, single-row
  chrome, collapsed icon rail, confirmed kill, ordinary `exit`, and authoritative
  tab cleanup are implemented and verified. The root-group sizing correction is
  live with served-selector evidence. The global five-zone/single-view
  correction is deployed as healthy zero-restart image `sha256:a7f2997f...`;
  predecessor `sha256:a2d7f313...` is preserved as
  `tmux-mobile:pre-global-snap-20260830`. The served bundle contains all five
  labels and one `drop-guidance` implementation, the HTTPS protocol check and
  direct-backend denial pass, and all six tmux sessions retain their pre-rollout
  window and attachment counts. Physical owner acceptance remains.
- Ubuntu evidence then showed tmuxctl's two-item HTML context menu overlapping
  tmux's fuller terminal-rendered menu. RCA
  `RCA/2026-08-30--desktop-duplicate-context-menus.md` traces the collision to
  tmuxctl's capture-phase `contextmenu` callback after tmux had already received
  the mouse event. The local correction reduces that hook to browser-default
  suppression and removes its callback plumbing, overlay state/markup, and CSS
  while retaining xterm/tmux event propagation and the typed topology API.
  Focused typecheck, all eleven frontend suites,
  the negative delivery guard, a production bundle without the removed menu
  strings, and canonical verification pass. Owner-approved image
  `sha256:07d570a1...` is now live, healthy, and at zero restarts; predecessor
  `sha256:a7f2997f...` is preserved as
  `tmux-mobile:pre-single-context-menu-20260830`. The served bundle contains no
  removed menu markers and all six sessions retain their recorded window and
  attachment counts. Physical acceptance remains.
- The owner requested desktop session rename and authorized its follow-up
  rollout. The implementation reuses the existing opaque-ID, CSRF-protected,
  rate-limited, audited rename endpoint; the expanded sidebar exposes a pencil
  control, and inventory reconciliation changes every open label without
  unmounting xterm. A direct request-contract test proves encoded target, fixed
  POST route, name-only JSON, and CSRF header; navigation tests prove label
  reconciliation and stable unchanged state. Production bundle inspection, all
  twelve frontend suites, 41 Desktop, 55 Server integration, 27 Core, 26
  Infrastructure plus five intentional skips, shell suites, and Compose
  assertions pass. Owner-authorized image `sha256:0370d1bc...` is now live,
  healthy, and at zero restarts; the prior single-menu image
  `sha256:07d570a1...` is preserved as
  `tmux-mobile:pre-desktop-rename-20260830`. Served JS contains both rename UI
  markers and no duplicate-menu markers, compatibility remains protocol 1,
  direct backend traffic remains denied with 426, bounded startup logs contain
  no failure, and all six sessions retain their exact pre-rollout window and
  attachment counts. Physical owner acceptance remains.
- The owner simplified desktop session termination from typed-name confirmation
  to a two-click flow. Clicking × now opens an accessible modal that identifies
  the session, defaults focus to Cancel, and performs no mutation until **Kill
  session** is clicked; busy state prevents duplicate submission. The protected
  inventory-resolved kill endpoint and detach/`exit` semantics are unchanged.
  Focused typecheck, all twelve frontend suites, the delivery guard, production
  bundle inspection, and canonical verification pass with 41 Desktop, 56 Server
  integration, 27 Core, and 26 Infrastructure tests plus five intentional
  skips. Owner-approved image `sha256:82b787f4...` is now live, healthy, and at
  zero restarts; prior capacity-fix image `sha256:1fbc3c0a...` is preserved as
  `tmux-mobile:pre-two-click-kill-20260830`. The served bundle contains the
  confirmation copy and no typed-name prompt, protocol 1 and backend 426 denial
  pass through a forced tailnet address with hostname/TLS validation, startup
  logs contain no failure, and all five pre-rollout sessions retain exact window
  and attachment counts. `resolvectl` resolves the MagicDNS name to the expected
  tailnet address, but sandboxed `curl` reports `Could not resolve host`; native
  connection and physical behavior remain owner acceptance.
- Unit 6 is paused only at owner/external boundaries: local source builds,
  Linux launch/identity, Ubuntu launcher, Apple Silicon app-bundle structure,
  compatibility, TLS, tests, docs, rollback, and review evidence are complete;
  the Ubuntu launcher is installed, while actual Ubuntu pinning, Apple Silicon
  launch/pinning, and owner interaction acceptance remain required.
- Physical launch after the two-click rollout reproduces the generic connection
  error. RCA
  `RCA/2026-08-31--desktop-magicdns-bypassed-by-resolver-stub.md` proves
  systemd-resolved and Tailscale know the correct MagicDNS route, while ordinary
  NSS clients bypass it because the managed stub target contains public DNS
  servers instead of `127.0.0.53`. The live app remains healthy and forced-address
  TLS/protocol checks pass. Host DNS service restart requires explicit approval.
- Ubuntu title-bar maximize is tracked independently from transport reconnect
  behavior in `RCA/2026-08-31--desktop-titlebar-maximize-misses-refit.md`.
  Photino native size, maximize, and restore events now cross the shell/web
  boundary and invoke every mounted terminal's existing settled-fit scheduler;
  frontend and delivery-contract verification pass, with physical maximize and
  restore acceptance still required.
- Rapid reconnect cycling is separately traced in
  `RCA/2026-08-31--desktop-reconnect-backoff-resets-on-handshake.md`. Terminal
  and inventory sockets now reset retry history only after ten seconds of
  stability; short-lived handshakes advance the existing bounded exponential
  backoff instead of allocating a new connection every second indefinitely,
  and stale socket-close callbacks cannot overwrite a newer healthy state.
