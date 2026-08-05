# Goal: Opt-in Terminal TUI Scrolling

Status: paused
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-04
Proposal: `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
Review Boundary: merge from `feat/c012-opt-in-tui-scroll` into `main`

## Outcome

Terminal swipes continue to navigate local tmux history by default, while an
accessible, non-persistent terminal button lets the owner explicitly route
bounded swipes as mouse-wheel input to foreground TUIs such as Claude Code and
mitmproxy.

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
- [x] AC3 — Enabled vertical swipes dispatch one to three directionally correct
  xterm wheel events and do not invoke the tmux-history protocol.
  - Evidence: frontend route tests pass; a temporary headless-Chrome xterm 6
    probe encoded synthetic wheel-up as SGR `ESC [ < 64 ; ... M`; isolated tmux
    forwarded that report to the alternate-screen mouse-aware program.
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
- [ ] AC6 — Documentation, rollback, review, and canonical verification agree
  with the implemented behavior and security posture.
  - Evidence: pending docs inspection, Review Record, and `./scripts/verify.sh`.

## Authority Envelope

### May Continue Without Asking

- Approved, local, reversible T0/T1 edits, tests, builds, isolated tmux probes,
  documentation, commits, and review artifacts required by this goal.
- Create and destroy only uniquely named isolated test tmux sockets/sessions.

### Must Pause for Approval

- Scope expansion, arbitrary or app-specific input, automatic activation,
  persistence, destructive or irreversible actions, owner-pane test input,
  remote/publication actions, deployment or production effects, security/privacy
  uncertainty, compatibility breaks, or any unapproved T2/T3 implementation.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Routing contract | completed | Pure bounded model selects unchanged history messages or wheel descriptors. | Frontend unit tests passed |
| 2. Thin-slice UI | completed | Default-off accessible toggle switches routing and resets safely. | Typecheck and production build passed |
| 3. Cross-boundary evidence | in_progress | Isolated xterm/tmux probes observe both owners; docs and review complete. | Local evidence passed; physical iPhone pending |

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

## Discoveries

- The current custom touch handler bypasses xterm's negotiated mouse path and
  always serializes a backend history operation.
- Application scrolling is terminal input and therefore cannot retain the prior
  claim that every terminal history gesture is PTY-input-free.
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
- Bound application scrolling to one to three wheel events per completed gesture.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: resolved; the isolated fake TUI initially omitted production
  `mouse on` and raw terminal mode. Each changed setup cause was corrected, and
  the final four-test run passed.

## Next Action

- Obtain explicit owner approval before deploying the verified C012 test build
  to the existing tailnet environment for physical-iPhone acceptance.

## Pause Conditions

- Pause if xterm requires private APIs, Safari rejects bounded synthetic wheel
  events without a safe public alternative, application routing requires raw or
  app-specific input, tests would touch an owner tmux pane, or any Authority
  Envelope boundary is reached.

## Outcomes

- Local implementation, documentation, canonical verification, negotiated xterm
  encoding, and isolated real-tmux evidence are complete.
- Physical-iPhone acceptance, final review decision, and any merge or deployment
  remain pending at explicit owner-controlled boundaries.
