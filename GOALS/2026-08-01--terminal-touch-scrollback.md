# Goal: Terminal Touch Scrollback

Status: completed
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-01
Proposal: `PROPOSALS/2026-08-01--terminal-touch-scrollback.md`
Review Boundary: merge from `feat/c008-terminal-touch-scrollback` into
`feat/c007-terminal-clipboard-paste`

## Outcome

The owner can swipe naturally through terminal scrollback on an iPhone and use
visible controls to return to live output without affecting tmux input.

## Non-Goals

- Do not change tmux history, browser scrollback depth, selection, zoom, backend
  contracts, or send validation input to user sessions.

## Acceptance Criteria

- [x] AC1 — One-finger vertical drag scrolls older/newer output naturally.
  - Evidence: `terminalScroll.test.ts` verifies drag-down scrolls toward older
    output and drag-up scrolls toward newer output.
- [x] AC2 — Tap, horizontal, and multi-touch paths do not scroll.
  - Evidence: threshold and axis-lock unit cases pass; viewport wiring resets
    rather than scrolling when the touch count is not one.
- [x] AC3 — Gesture and history controls never send terminal input.
  - Evidence: gesture/control handlers invoke only xterm `scrollLines`,
    `scrollPages`, and `scrollToBottom`; inspection found no `send` call path.
- [x] AC4 — Accessible Older and Latest controls supplement gestures.
  - Evidence: production bundle contains labeled Older/Latest buttons and the
    older-output status announcement.
- [x] AC5 — Focused gesture tests and canonical verification pass.
  - Evidence: `./scripts/verify.sh` passed 44 .NET tests, skipped the one
    opt-in isolated PTY test, and passed both frontend unit suites and typecheck.
- [x] AC6 — Live bundle/health/listener validation and Change Review pass.
  - Evidence: container healthy; HTTPS live endpoint and terminal bundle return
    200; listener is exactly `100.85.13.102:8780`; Review Record is ready with
    physical-iPhone follow-up.

## Authority Envelope

### May Continue Without Asking

- Implement, build, and test the approved frontend gesture/control feature.
- Rebuild only the current Tailscale Serve test container and run read-only
  bundle, health, and listener checks.

### Must Pause for Approval

- Attaching or sending validation input to a user tmux session, changing backend
  or scrollback limits, intercepting pinch zoom, broadening network exposure,
  changing tailnet policy, merging, or pushing.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Gesture | completed | Natural bounded scroll math and wiring compile | unit/build passed |
| 2. Alternatives | completed | Visible controls and docs agree | inspection/build passed |
| 3. Deploy/review | completed | Live bundle, full gate, review pass | HTTPS/verify/review passed |

## Progress

- 2026-08-01: owner explicitly requested terminal vertical swipe scrollback.
- 2026-08-01: added bounded axis-lock/line-accumulation helpers, xterm viewport
  touch wiring, visible history navigation, and user-facing guidance.
- 2026-08-01: focused tests, typecheck, production build, and canonical
  verification passed.
- 2026-08-01: committed `bfcebd1`, rebuilt the Tailscale Serve profile, and
  confirmed healthy HTTPS service plus exact-IP listener and live bundle.
- 2026-08-01: Change Review passed with physical-iPhone validation recorded as
  a non-blocking follow-up.

## Evidence

- Frontend unit runner: 2/2 test files passed.
- Canonical gate: 44 .NET tests passed, 1 opt-in PTY test skipped; frontend
  typecheck and both unit suites passed.
- Production bundle: `TerminalView-DYP_0u21.js` contains the history guidance
  and labeled controls.
- Live: HTTPS health and hashed bundle returned 200; live bundle contains swipe
  guidance, older-output status, and both accessible button labels.
- Network: Compose reports healthy with only
  `100.85.13.102:8780->5179/tcp`; `ss` confirms the exact listener.

## Discoveries

- xterm exposes public `scrollLines`, `scrollPages`, and `scrollToBottom` APIs.
- Current browser scrollback is bounded at 2,000 lines.
- The app has no explicit terminal touch-scroll wiring today.

## Decisions

- Use a six-pixel axis-lock threshold and accumulate movement into 18-pixel line
  steps.
- Prevent the browser default only after recognizing a one-finger vertical drag.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- Owner validates natural gesture feel on a physical iPhone without sending
  sensitive terminal input.

## Pause Conditions

- Pause if scrolling requires terminal input, backend changes, global iOS gesture
  interception, or real-session validation input.

## Outcomes

- Terminal mode now supports natural one-finger scrollback and visible
  Older/Latest navigation without writing to the PTY.
- The verified build is live behind Tailscale Serve at the existing HTTPS URL.
