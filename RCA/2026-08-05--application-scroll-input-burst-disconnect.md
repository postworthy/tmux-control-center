# RCA: Application Scroll Burst Disconnects the Terminal

Date: 2026-08-05
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
Related Commit(s): `b1c7224`, `be22213`, `a0ff587`

## Symptom

- The owner reported that App Scroll often stops working and the terminal changes
  to disconnected, requiring the Reconnect action.
- The regression appeared after velocity scaling raised one gesture from at most
  three synthetic wheel events to as many as 72.

## Reproduction

1. Open a mouse-aware TUI through the deployed PWA and enable App Scroll.
2. Perform a sufficiently long or fast swipe to produce more than 64 wheel
   events inside one gesture.
3. Observe the terminal disconnect and display its reconnect state.
4. Inspect the live application logs. Three consecutive affected connections to
   the reported session each show `Terminal input rate limit exceeded`, followed
   by PTY cleanup and terminal WebSocket disconnection.

The failure is also deterministic from the existing contracts: C012 permits 72
wheel events; xterm synchronously emits one `onData` value for every negotiated
wheel event; `TerminalView` immediately calls `send` for every value; and the
server's per-connection bucket starts with 64 input-message tokens. The 65th
message is rejected with Policy Violation and the socket is closed.

## Root Cause

- Implementation layer: the modifier-isolation guard forwarded each xterm wheel
  report through `send` independently while synthetic dispatch was in progress.
  Raising the gesture cap to 72 therefore also raised the WebSocket message burst
  to 72, although the complete encoded input is only hundreds of bytes and fits
  safely in one existing bounded input envelope.
- Contract layer: the velocity proposal bounded wheel-event count but did not
  bound or specify WebSocket message amplification per user gesture against the
  established 64-message input bucket.
- Verification layer: pure routing tests counted signed wheel descriptors, the
  xterm probe tested only one event, and server integration independently proved
  that the input limiter closes abusive bursts. No test joined these boundaries
  by asserting the number of serialized input messages produced by a maximum
  application-scroll gesture.
- Evidence: live logs identify the precise close reason three times; server code
  initializes `MaxTerminalInputMessagesPerSecond` to 64 and closes on exhaustion;
  frontend code sends once inside each guarded `onData` callback; C012 tests allow
  72 events.

## Corrective Action

- During one synthetic wheel dispatch, buffer the ordered xterm `onData` chunks
  instead of sending each immediately.
- After xterm finishes the synchronous gesture dispatch, concatenate the chunks
  and pass them once through the existing bounded `serializeTerminalInput` path.
- Preserve negotiated xterm/tmux bytes, 1x–4x velocity behavior, the 72-event cap,
  modifier isolation, and the server's 64-message protection unchanged.

## Preventive Controls

- Regression test: a maximum 72-report gesture coalesces into one terminal input
  value and one bounded serialized WebSocket message while preserving byte order.
- Cross-boundary guard: application-generated input gestures must specify both
  event count and resulting WebSocket message count; one gesture may not amplify
  into a limiter-exhausting message burst.
- Runtime evidence: after deployment, exercise repeated maximum gestures and
  confirm no new `terminal.input.rate-limit` audit/log event and no reconnect.

## Narrower Next Action

Create a corrective C014 goal, add the coalescing regression contract, implement
one-gesture buffering in `TerminalView`, then run focused, canonical, production,
and physical-device verification before replacing the live test image.
