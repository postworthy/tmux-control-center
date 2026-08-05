# Goal: Opt-in Terminal TUI Scrolling

Status: active
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-04
Proposal: `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
Review Boundary: merge from `feat/c012-opt-in-tui-scroll` into `main`

## Outcome

Terminal swipes and Older/Latest continue to navigate local tmux history by
default, while an accessible, non-persistent terminal button lets the owner
explicitly route swipe-proportional and toolbar-directed mouse-wheel input to
foreground TUIs such as Claude Code and mitmproxy.

## Non-Goals

- Do not auto-detect or auto-enable application scrolling.
- Do not add app-specific commands, remote control, arbitrary input, persistence,
  deployment, tmux configuration changes, or non-wheel mouse emulation.

## Acceptance Criteria

- [x] AC1 — Default terminal swipes retain the existing bounded tmux-history
  message path and send no PTY input.
  - Evidence: frontend routing regression test passes and observes the exact
    existing bounded history JSON while application scrolling is off.
- [x] AC2 — An accessible toolbar toggle enables application scrolling only by
  explicit action and resets off on terminal entry and connection loss.
  - Evidence: source/build inspection finds a default-false ref/state,
    `aria-pressed`, visible active styling, and resets at terminal initialization,
    disconnect, and exit; typecheck and production build pass.
- [ ] AC3 — Enabled vertical swipes dispatch one wheel event per 18 pixels of
  movement up to a 24-event cap, with correct direction and no tmux-history
  request.
  - Evidence: revised frontend routing tests pass for proportional direction,
    magnitude, and cap; physical-iPhone check remains pending.
- [ ] AC7 — Enabled Older/Latest dispatch fixed 12-event wheel-up/wheel-down
  bursts through the same xterm path, while disabled behavior remains exact.
  - Evidence: focused routing tests, typecheck, and production build pass;
    physical-iPhone check remains pending.
- [x] AC4 — Tap, horizontal, multi-touch, pinch, typing, clipboard, reconnect,
  and cleanup paths remain compatible.
  - Evidence: frontend regression tests and `./scripts/verify.sh` pass; no
    backend or WebSocket contract changed.
- [x] AC5 — Real isolated tmux evidence distinguishes normal tmux history from
  mouse-aware alternate-screen application scrolling.
  - Evidence: four LinuxIntegration tests pass in a disposable .NET 10/tmux
    container. Existing history test observes positive `scroll_position`; new
    test observes `alternate_on=1`, `mouse_any_flag=1`, SGR mode, and forwarded
    wheel input. All sockets are uniquely named and no owner pane receives input.
- [x] AC6 — Documentation, rollback, review, and canonical verification agree
  with the implemented behavior and security posture.
  - Evidence: docs and pending Review Record describe default versus enabled
    input semantics; canonical verification passes; the previous live image is
    preserved as `tmux-mobile:pre-c012-rollback` and post-deployment health,
    readiness, HTTPS, bundle, tmux compatibility, and backend-denial checks pass.
- [x] AC8 — Production packaging stamps a release-specific service worker and
  excludes obsolete hashed application bundles from the runtime image.
  - Evidence: the live image's worker digest is `4fba5613...`; the revised
    image's is `b2498489...` with cache ID `15e34998b5097601`. Runtime inspection
    finds only the current main and terminal hashed bundles plus their compressed
    forms, with no pre-C012 or initial-C012 JavaScript.

## Authority Envelope

### May Continue Without Asking

- Approved, local, reversible T0/T1 edits, tests, builds, isolated tmux probes,
  documentation, commits, and review artifacts required by this goal.
- Create and destroy only uniquely named isolated test tmux sockets/sessions.
- Owner-approved deployment of the verified C012 image to the existing
  `tmux-mobile-tailnet-serve` test environment, preserving the current image as
  a rollback tag and verifying its established security/health boundaries.

### Must Pause for Approval

- Scope expansion, arbitrary or app-specific input, automatic activation,
  persistence, destructive or irreversible actions, owner-pane test input,
  remote/publication actions, deployment or production effects, security/privacy
  uncertainty, compatibility breaks, or any unapproved T2/T3 implementation.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Routing contract | completed | Pure bounded model selects unchanged history messages or proportional/fixed wheel descriptors. | Revised frontend unit tests passed |
| 2. Thin-slice UI | completed | Default-off accessible toggle switches swipe and toolbar routing and resets safely. | Typecheck and production build passed |
| 3. Release boundary | completed | Production service worker identity changes and runtime image contains only current bundles. | Two-revision image inspection passed |
| 4. Cross-boundary evidence | pending | Isolated xterm/tmux probes observe both owners; docs and review complete. | Local evidence passed; physical iPhone pending |

## Progress

- 2026-08-04: owner approved preserving current behavior as the default and
  requiring an explicit terminal button before mouse-wheel translation.
- 2026-08-04: live diagnosis found Claude Code and mitmproxy in alternate-screen
  mouse modes with `mouse_any_flag=1`; tmux global mouse support is already on.
- 2026-08-04: research selected negotiated xterm wheel events plus tmux's dynamic
  mouse-owner routing instead of process/title detection.
- 2026-08-04: implemented the pure routing model, accessible App Scroll control,
  state resets, modifier-safe wheel dispatch, and isolated mouse-forwarding test.
- 2026-08-04: frontend unit/type checks, production container build, headless
  xterm encoding probe, four isolated Linux tests, and canonical verification
  pass. Execution paused before the unapproved deployment boundary.
- 2026-08-04: owner explicitly approved deployment for physical-iPhone testing;
  execution resumed against only the existing tailnet Serve test environment.
- 2026-08-04: preserved live image `sha256:d2b52a...` as
  `tmux-mobile:pre-c012-rollback`, built `tmux-mobile:c012-opt-in-tui-scroll`,
  and recreated only the existing Compose app. New image `sha256:00eb6c...` is
  healthy; execution paused for owner physical-iPhone acceptance.
- 2026-08-04: owner reported that App Scroll was not visible. RCA found that
  the pre-C012 and C012 images ship byte-identical service workers, so an open
  PWA receives no update prompt, while stale hashed bundles retained in the new
  image allow the old UI to keep running. No corrective redeployment has been
  attempted; owner force-close/reopen confirmation is pending.
- 2026-08-04: owner force-close/reopen made App Scroll visible, confirming the
  stale-runtime RCA. Owner then approved proportional swipe movement and
  application-mode routing for Older/Latest; the goal resumed for that local
  T1 correction, without deployment authority for the revised build.
- 2026-08-04: implemented one wheel event per consumed 18-pixel touch line up to
  24 events, fixed 12-event Older/Latest application bursts, shared xterm wheel
  dispatch, dynamic accessibility labels, and application-mode Latest enablement.
  Focused tests, typecheck, production server-build image, and canonical
  verification pass.

## Evidence

- Live read-only formats: Claude Code and mitmproxy reported alternate screen,
  empty local history, SGR mouse mode, and `mouse_any_flag=1`.
- Local tmux root binding routes `WheelUpPane` through `send-keys -M` when
  `pane_in_mode` or `mouse_any_flag` is true, otherwise enters copy mode.
- xterm 6 source binds negotiated wheel events through its mouse service without
  rejecting synthetic events by `isTrusted`.
- Headless Chrome observed `data-result=pass` and report bytes
  `27,91,60,54,52,...,77`, an SGR wheel-up event emitted by installed xterm 6.
- `TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 dotnet test ...` passed four isolated
  tests in the disposable .NET 10/tmux container after the fake TUI correctly
  matched real TUI raw-mode behavior.
- Exact canonical command passed with 24 Core, 12 Infrastructure (four isolated
  tests skipped there), 33 Server integration, and both frontend test files.
- Live tailnet Serve deployment: HTTPS liveness and root return 200, the served
  terminal bundle contains `App Scroll`, direct backend root remains 426,
  container readiness returns 200, tmux 3.4 can enumerate 12 current sessions,
  and startup logs show no new errors.
- Revised frontend tests observe 5 ticks for five consumed swipe lines, 12 ticks
  for twelve lines, the 24-event cap, exact default Older/Latest history JSON,
  and signed 12-event application button bursts. Typecheck and the production
  `server-build` image pass.
- 2026-08-04: implemented deterministic service-worker stamping from generated
  `index.html` and cleaned only the temporary container webroot before copying
  generated assets. Full runtime image `sha256:72e88f...` contains only current
  hashed bundles; its worker differs from the deployed image. Canonical
  verification passes after the packaging correction.
- Revised canonical verification passes with 24 Core, 12 Infrastructure (four
  isolated tests skipped), 33 Server integration, frontend typecheck/tests, and
  Compose validation when run with the repository's local .NET 10 SDK.
- Revised runtime image `sha256:72e88f...` contains current
  `index-CCBStx_A.js` and `TerminalView-DoAYawVj.js` only (plus CSS and compressed
  forms). Its stamped worker cache is
  `tmux-mobile-shell-15e34998b5097601`, with digest `b2498489...`, versus the
  deployed worker's `4fba5613...` digest.

## Discoveries

- The current custom touch handler bypasses xterm's negotiated mouse path and
  always serializes a backend history operation.
- Application scrolling is terminal input and therefore cannot retain the prior
  claim that every terminal history gesture is PTY-input-free.
- The PWA update banner cannot detect application releases while the service
  worker remains byte-identical, and the image build retains obsolete hashed
  bundles because generated web output overlays tracked `wwwroot` contents.
- No backend or WebSocket contract change is required for the planned thin slice.
- A mouse-aware test program must set raw terminal mode; the first fixture was
  line buffered and could not observe a wheel report until corrected.

## Decisions

- Keep application scrolling default-off, explicit, visually indicated, and
  non-persistent; reset it on terminal entry and connection loss.
- Preserve the existing history request path byte-for-byte while the toggle is
  off.
- Use xterm's negotiated mouse encoder rather than hand-built escape sequences,
  process names, pane titles, or remote-program discovery.
- Scale application scrolling at one wheel event per 18 pixels, capped at 24
  events per gesture; use fixed 12-event bursts for application-mode
  Older/Latest controls.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: resolved; the isolated fake TUI initially omitted production
  `mouse on` and raw terminal mode. Each changed setup cause was corrected, and
  the final four-test run passed.

## Next Action

- Commit the verified proportional-scroll and release-boundary correction, then
  pause for explicit approval before replacing the deployed test image.

## Pause Conditions

- Pause if xterm requires private APIs, Safari rejects bounded synthetic wheel
  events without a safe public alternative, application routing requires raw or
  app-specific input, tests would touch an owner tmux pane, or any Authority
  Envelope boundary is reached.

## Outcomes

- Local implementation, documentation, canonical verification, negotiated xterm
  encoding, and isolated real-tmux evidence are complete.
- The verified C012 image is healthy in the existing tailnet Serve test
  environment with a validated pre-C012 rollback tag.
- Physical-iPhone acceptance, final review decision, and any merge or push remain
  pending at explicit owner-controlled boundaries.
- Physical testing exposed a release-lifecycle defect not covered by the live
  network bundle check; `RCA/2026-08-04--deployed-terminal-control-not-visible.md`
  records the evidence and corrective boundary.
