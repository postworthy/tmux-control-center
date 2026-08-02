# Proposal: Terminal Touch Scrollback

Date: 2026-08-01
Owner: Human Partner and AI Agent
Risk Class: T2
Related Context: owner requested vertical swipe scrolling in terminal mode
Roadmap Item: C008
Planned Branch: `feat/c008-terminal-touch-scrollback`
Expected Commit Count: 2

## Objective

Let the owner naturally drag through recent xterm.js output on an iPhone and
return to live output without sending terminal input.

## Scope

In scope:

- One-finger vertical gesture recognition confined to the terminal viewport.
- Natural content direction: drag down for older output and up for newer output.
- Pixel-to-line accumulation with a movement threshold and vertical-axis lock.
- Visible Older and Latest controls, status semantics, tests, built PWA assets,
  and current Tailscale Serve test-container replacement.

Out of scope:

- Changing tmux history, increasing the 2,000-line browser scrollback, terminal
  selection redesign, pinch-zoom interception, or sending validation input to a
  user tmux session.

## Acceptance Criteria

- [x] One-finger vertical dragging scrolls xterm output in natural direction.
- [x] Taps, horizontal movement, and multi-touch do not trigger scrollback.
- [x] Scrolling does not send PTY/WebSocket input or change tmux state.
- [x] Older and Latest controls provide accessible non-gesture navigation.
- [x] Focused direction/threshold/remainder tests and canonical verification pass.
- [x] The deployed HTTPS terminal bundle, health, exact listener, and Change
  Review pass.

## Verification Plan

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
```

Live validation checks the hashed terminal bundle and health without attaching
to or sending input into a user session.

## Change Review Plan

- Review Boundary: merge into `feat/c007-terminal-clipboard-paste`
- Planned Review Record: `REVIEWS/2026-08-01--terminal-touch-scrollback.md`

## Decomposition Plan

1. Add tested vertical gesture math and xterm viewport wiring.
2. Add Older/Latest alternatives, accessibility labels, and documentation.
3. Build, deploy, verify, and review the exact committed bundle.

Thin slice: dragging downward over terminal output moves into older scrollback
without producing a WebSocket input message.

## Rollback Plan

Revert C008, rebuild `compose.tailscale-serve.yaml`, and confirm the previous
terminal bundle and HTTPS health. No persistent state migration exists.

## Risks and Mitigations

- Gesture/input conflict: require one touch, vertical dominance, and movement
  threshold; do not focus or send terminal data from scroll handlers.
- Browser page gesture conflict: prevent default only after a vertical terminal
  gesture is recognized and keep listeners confined to the viewport.
- Inaccessible gesture-only behavior: include visible Older and Latest buttons.
- Live-session impact: validate bundle/UI paths without attaching to user tmux.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-01 America/Chicago
