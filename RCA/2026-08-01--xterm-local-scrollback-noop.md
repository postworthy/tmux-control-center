# RCA: xterm-Local Scrollback Is a No-op for Attached tmux History

Date: 2026-08-01
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-01--terminal-touch-scrollback.md`
Related Commit(s): `bfcebd1`, `9d80a02`

## Symptom

- The owner reported that vertical terminal swiping did not work on the deployed
  iPhone PWA.
- The same implementation's Older and Latest controls targeted the same xterm
  browser buffer, so they could not provide tmux pane history either.

## Reproduction

1. Attach xterm.js to tmux through the application's PTY terminal endpoint.
2. Attempt to navigate earlier tmux output with the deployed gesture or Older
   control.
3. Observe no earlier pane history despite the handler calling
   `Terminal.scrollLines` or `Terminal.scrollPages`.

The repository owner reproduced the failure on the target iPhone. A source-level
boundary reproduction shows why: xterm creates its alternate buffer with
scrollback disabled, and tmux documents that it does not keep the outside
terminal's scrollback consistent. tmux pane history is instead navigated through
tmux copy mode.

## Evidence

- `@xterm/xterm/src/common/buffer/BufferSet.ts` constructs the alternate buffer
  with `new Buffer(false, ...)` and states that it must never have scrollback.
- `@xterm/xterm/src/common/buffer/Buffer.ts` limits a buffer without scrollback
  to viewport rows, leaving no `ybase` for xterm scroll methods to navigate.
- The official tmux FAQ says tmux does not attempt to keep terminal scrollback
  consistent and it is likely incomplete.
- The deployed implementation invokes only xterm `scrollLines`, `scrollPages`,
  and `scrollToBottom`; it never requests tmux copy mode.
- The passing test `terminalScroll.test.ts` covers only threshold, direction,
  and pixel remainder math. It does not instantiate xterm, activate an alternate
  buffer, attach tmux, or assert a tmux history operation.

## Root Cause

- Implementation layer: the feature treated xterm's browser buffer as the
  authoritative history while the architecture attaches a real tmux terminal
  client. The requested history belongs to tmux and must be read through tmux
  copy mode.
- Verification layer: acceptance evidence stopped at pure gesture math and
  bundle-string inspection. No test crossed the frontend/WebSocket/tmux-control
  boundary, and physical-iPhone validation was incorrectly left as a follow-up
  after deployment rather than required evidence for gesture completion.
- This explains both the symptom and the misleading green checks: the gesture
  handler ran successfully, but its target buffer had no relevant history.

## Corrective Action

- Replace local xterm scrolling with a typed, bounded terminal WebSocket history
  message.
- Resolve the already validated session target server-side and use explicit tmux
  argument arrays to enter copy mode, move up/down, and cancel back to live
  output.
- Send one bounded operation at gesture completion rather than spawning a tmux
  command for every touchmove.
- Cancel app-entered history mode on Latest, terminal exit, and disconnect.

## Preventive Controls

- Add a frontend protocol test for bounded page-history messages and natural gesture
  direction.
- Add server integration coverage proving a history message reaches a fake tmux
  controller and writes no bytes to the PTY input stream.
- Add infrastructure coverage for exact `copy-mode`/`send-keys -X` argument
  arrays and identifier resolution.
- Require physical-iPhone gesture confirmation before the corrective goal is
  considered fully validated; automated verification may establish deployment
  readiness but not touch acceptance.

## Narrower Next Action

Implement C009 as a tmux-backed history control protocol without changing pane
history depth, accepting arbitrary commands, or attaching automated checks to a
user session.
