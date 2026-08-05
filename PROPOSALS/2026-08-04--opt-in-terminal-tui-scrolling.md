# Proposal: Opt-in Terminal TUI Scrolling

Date: 2026-08-04
Owner: Human Partner and AI Agent
Risk Class: T1
Related Issue/Context: Claude Code and mitmproxy own scroll navigation while in
alternate-screen mouse mode, leaving local tmux history empty.
Roadmap Item: C012
Planned Branch: `feat/c012-opt-in-tui-scroll`
Expected Commit Count: 4

## Objective

Keep the existing tmux-backed terminal swipe behavior as the default while
allowing the owner to explicitly and temporarily route vertical swipes as
mouse-wheel input to foreground TUIs such as Claude Code and mitmproxy.

## Scope

In scope:

- Add an accessible terminal toolbar toggle for application-directed scrolling.
- Default the toggle to off and never persist it across terminal views,
  disconnects, reconnects, reloads, or sessions.
- While off, retain the current typed WebSocket tmux-history protocol unchanged.
- While on, translate vertical swipe distance proportionally into a bounded
  number of synthetic wheel events over xterm so xterm and tmux negotiate and
  route the mouse protocol.
- While on, route Older and Latest through the same application-wheel path as
  fixed directionally equivalent wheel-up and wheel-down bursts; while off,
  preserve their existing tmux-history commands exactly.
- Preserve horizontal, tap, multi-touch, pinch, typing, clipboard, and terminal
  lifecycle behavior.
- Add focused frontend coverage, isolated terminal/tmux evidence, documentation,
  and a Change Review.
- Correct the confirmed deployment boundary by stamping each production service
  worker from its generated asset graph and copying generated web assets into a
  clean container webroot.

Out of scope:

- Automatic detection or activation based on process names, titles, SSH, or
  alternate-screen state.
- Persisting the toggle or changing its state across terminal sessions.
- App-specific Claude Code or mitmproxy commands, key bindings, remote adapters,
  or arbitrary mouse/text payloads.
- Changing tmux configuration, history limits, API authorization, network
  exposure, or production state. The owner separately approved replacement of
  only the existing tailnet Serve test container on 2026-08-04.
- Implementing click, drag-selection, hover, horizontal wheel, or general touch
  mouse emulation.

## Expected Files Touched

- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `src/TmuxMobile.Web/src/TerminalView.tsx`
- `src/TmuxMobile.Web/src/terminalScroll.ts`
- `src/TmuxMobile.Web/src/styles.css`
- `src/TmuxMobile.Web/tests/terminalScroll.test.ts`
- `src/TmuxMobile.Web/scripts/version-service-worker.mjs`
- `src/TmuxMobile.Web/public/service-worker.js`
- `Dockerfile`
- `docs/architecture.md`
- `docs/security.md`
- `README.md`
- `STATUS.md`
- `REVIEWS/2026-08-04--opt-in-terminal-tui-scrolling.md`

## Acceptance Criteria

- [ ] With application scrolling off, a vertical swipe serializes the same
  bounded `older`/`newer` history request as before and never sends PTY input.
- [ ] The terminal toolbar exposes an accessible application-scroll toggle whose
  default and reset state is off and whose enabled state is visibly and
  programmatically indicated.
- [ ] With application scrolling on, a completed vertical swipe dispatches one
  wheel event per 18 pixels of movement, capped at 24 directionally correct
  events through xterm, and does not send a tmux-history request.
- [ ] With application scrolling on, Older dispatches a fixed 12-event wheel-up
  burst and Latest dispatches a fixed 12-event wheel-down burst through the
  same xterm path; with the mode off, both retain their exact history behavior.
- [ ] Tap, horizontal, multi-touch, pinch, typing, clipboard, reconnect, and
  terminal cleanup behavior remain unchanged.
- [ ] An isolated real-tmux check proves ordinary panes retain tmux copy-mode
  scrolling while a mouse-aware alternate-screen pane receives wheel input.
- [ ] Focused checks and `./scripts/verify.sh` pass; docs explain the explicit
  mode, security change, rollback, and limitations.
- [ ] A production image contains only its current hashed application bundles,
  and its service-worker bytes change with the generated root asset identity so
  an installed PWA can detect the release.

## Verification Plan

Commands:

```bash
./scripts/verify.sh
```

Focused checks:

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run build
dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
```

Pass means:

- Canonical verification exits 0.
- Frontend tests distinguish default history routing from explicit wheel routing.
- An isolated tmux/PTY probe observes the correct owner receive each gesture.
- No terminal content, arbitrary payload, or persistent mode state is introduced.

## Change Review Plan

- Review Boundary: merge from `feat/c012-opt-in-tui-scroll` into `main`
- Planned Review Record:
  `REVIEWS/2026-08-04--opt-in-terminal-tui-scrolling.md`
- Reviewer/approver expectation: owner confirms physical-iPhone default and
  opt-in behavior before merge or deployment.

## Git Plan

- Branch command: `git switch -c feat/c012-opt-in-tui-scroll`
- Commit subject pattern: `feat(terminal): add opt-in TUI swipe scrolling`
- Required commit trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C012`
  - `Proposal: PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
- Planned merge method: `git merge --no-ff feat/c012-opt-in-tui-scroll`

## Decomposition Plan (Required for T1/T2/T3)

Work units (ordered):

1. Routing contract and pure gesture model — Verify by frontend unit tests —
   Exit criteria: gesture distance and toolbar actions deterministically select
   unchanged history serialization or bounded wheel-event descriptors — Risk:
   T1 — Dependencies: xterm 6 wheel handling.
2. Thin-slice terminal UI — Verify by typecheck, build, DOM inspection, and an
   isolated xterm mouse-protocol probe — Exit criteria: accessible opt-in button
   switches routing, defaults off, and resets on disconnect — Risk: T1 —
   Dependencies: unit 1.
3. Cross-boundary verification and documentation — Verify by isolated real tmux,
   canonical verification, and documentation inspection — Exit criteria: both
   scroll owners are observed, risks are documented, and the review record is
   complete — Risk: T1 — Dependencies: unit 2.

Thin slice milestone:

- After unit 2, the local bundle exposes a default-off Application Scroll toggle
  and routes swipes through either the existing history message or xterm wheel
  handling without any backend/API change.

Dependencies and unknowns:

- Synthetic `WheelEvent` dispatch must be accepted by xterm 6 on iOS Safari;
  automated evidence reduces risk but physical-device acceptance remains needed.
- The foreground TUI decides what a wheel event means at the touched coordinate.

Intentional deferrals:

- Automatic mouse-owner discovery in the UI.
- Dedicated application-only PageUp/PageDown buttons; the existing
  Older/Latest controls are dual-routed by the explicit mode.
- Click, selection, hover, and remote-host integration.

## Rollback Plan

If this change causes regressions:

1. Revert the C012 feature and documentation commits.
2. Restore the prior `TerminalView` touch handler, which always emits the typed
   tmux-history message.
3. Validate rollback with frontend tests and `./scripts/verify.sh`.

## Risks and Mitigations

- Risk: an enabled swipe becomes foreground-application input rather than a
  read-only tmux-history operation.
  Mitigation: explicit default-off toggle, no persistence, reset on connection
  loss, fixed wheel semantics, bounded events, visible pressed state, and docs.
- Risk: synthetic wheel direction, magnitude, or coordinates differ on Safari.
  Mitigation: pure routing tests, xterm protocol probe, a 24-event gesture cap,
  fixed 12-event toolbar bursts, and required physical-iPhone acceptance before
  deployment.
- Risk: the TUI treats wheel input contextually.
  Mitigation: activation is explicit and the app does not claim that wheel input
  is read-only or equivalent to tmux history.

## Compatibility / Migration Notes

- API compatibility impact: none; the existing WebSocket contracts remain.
- Data/schema migration needed: no.
- Backward compatibility window: default behavior is unchanged indefinitely;
  application scrolling exists only while explicitly enabled in a terminal view.

## Observability / Debug Notes

- New logs/metrics/traces: none; terminal gestures and content remain unlogged.
- Detect failure through toggle pressed state, frontend routing tests, xterm
  `onData` output in an isolated probe, tmux `mouse_any_flag`, and copy-mode
  `scroll_position`.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-04, through the explicit request to preserve current
  behavior and require a terminal button before enabling mouse-wheel translation.
- Revised scope approved at: 2026-08-04, through the owner's request for wheel
  movement proportional to swipe distance and dual routing of Older/Latest
  while application scrolling is enabled.
