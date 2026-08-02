# Review Record: Terminal Touch Scrollback

Date: 2026-08-01
Review Boundary: merge from `feat/c008-terminal-touch-scrollback` into
`feat/c007-terminal-clipboard-paste`
Merge Method: `git merge --no-ff feat/c008-terminal-touch-scrollback`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-01--terminal-touch-scrollback.md`

## Decision

Ready with explicit physical-iPhone follow-up. Terminal touch gestures now move
through bounded xterm browser scrollback in natural direction, while accessible
Older and Latest controls expose the same navigation without gestures. Review
and live validation did not attach to or send input into a user tmux session.

## Commits in Scope

- `bfcebd1` feat(terminal): add touch scrollback navigation

The review-record commit is documentation-only evidence created afterward.

## Scope and Git

- [x] Work is on the approved C008 feature branch.
- [x] Diff contains only touch scrollback UX/tests, built PWA assets,
  documentation, and Tempo records.
- [x] Backend, authentication, terminal protocol, network exposure, browser
  scrollback depth, and tailnet configuration are unchanged.
- [x] Local environment, credentials, logs, caches, and runtime state are absent
  from the diff.
- [x] Only the approved Tailscale Serve test container was rebuilt.

## Acceptance Evidence

- [x] Six-pixel threshold and vertical-axis dominance avoid tap and horizontal
  false positives; ambiguous diagonals remain non-scrolling.
- [x] Drag-down produces negative xterm line movement toward older output;
  drag-up produces positive movement toward newer output.
- [x] Sub-line movement accumulates against an 18-pixel line step.
- [x] Multi-touch resets the gesture rather than scrolling.
- [x] Gesture and button handlers call only `scrollLines`, `scrollPages`, and
  `scrollToBottom`; they do not call terminal input serialization or WebSocket
  send paths.
- [x] Older and Latest buttons have descriptive accessible labels, and the UI
  announces when the viewport is not following live output.
- [x] The live hashed bundle contains swipe guidance, older-output status, and
  both accessible history labels.

## Verification

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
```

- Touch-scroll test: pass for threshold, axis lock, natural direction, and
  movement remainder.
- Frontend production build and typecheck: pass.
- Canonical gate: 24 Core, 7 Infrastructure, and 13 Server tests passed; one
  opt-in isolated PTY test skipped; both frontend unit files passed.

```text
container=healthy https_live=200 terminal_bundle=200
listener=100.85.13.102:8780
bundle_states=swipe_guidance,older_status,older_button,latest_button
```

## Findings

- Blocking/high: none.
- Medium: none.
- Low: automated touch math and live-bundle checks cannot reproduce the exact
  momentum and gesture feel of physical iPhone Safari. Owner validation remains
  required.
- Low: deliberate live checks did not open an interactive terminal, protecting
  the user's running sessions from test input.

## Compatibility and Rollback

- Existing taps, keyboard input, shortcut input, paste, reconnect, and terminal
  WebSocket behavior remain on their prior paths.
- Pinch zoom and horizontal touch behavior are not intercepted; only recognized
  one-finger vertical movement prevents browser default handling in the terminal
  viewport.
- REST, WebSocket schema, PTY lifecycle, persistent data, and deployment
  settings are unchanged.
- Rollback: revert C008, rebuild the Serve profile, then confirm HTTPS health
  and the prior hashed terminal bundle. No migration exists.

## Approvals

- Feature implementation and current test-container deployment: repository
  owner, 2026-08-01.
- Reviewer: Codex, evidence-backed local review.
- Status: ready with physical-iPhone follow-up.
- Merge/push: not authorized or implied.

## Follow-Ups

- On the iPhone, drag down over terminal output to reveal older lines, drag up
  toward live output, and confirm taps still focus the terminal normally.
- Replace the temporary weak login password as already recorded by C006.
