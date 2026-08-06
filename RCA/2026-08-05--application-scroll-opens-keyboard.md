# RCA: Application Scroll Opens the iPhone Keyboard

Date: 2026-08-05
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
Related Commit(s): `d3f6edb`, `b1c7224`, `e3e9e4a`

## Symptom

- After the C014 disconnect correction made scrolling reliable, the owner
  reported that every App Scroll gesture opens the iPhone software keyboard.
- Application-mode Older and Latest cause the same keyboard behavior.

## Reproduction

1. Open a terminal in the installed iPhone PWA and enable App Scroll.
2. Dismiss the software keyboard if it is already visible.
3. Swipe either direction; the keyboard opens when the gesture completes.
4. Dismiss it and press Older or Latest while App Scroll remains enabled; the
   keyboard opens again.

The source reproduces the shared cause deterministically: swipes and
application-mode Older/Latest all call `dispatchApplicationWheel`, which calls
`xterm.focus()` after dispatch. The App Scroll toggle independently calls
`terminal.current?.focus()`. Installed xterm 6 implements `focus()` by invoking
`this.textarea.focus({ preventScroll: true })`; that hidden textarea is the
terminal keyboard input element.

## Root Cause

- Implementation layer: application scrolling reused terminal focus restoration
  intended for typing-oriented shortcut controls. Calling xterm focus after
  wheel-only interactions focuses its hidden textarea, which iOS correctly
  interprets as a request to show the software keyboard.
- Interaction-contract layer: App Scroll specified mouse-wheel input semantics
  but did not explicitly require focus neutrality or distinguish wheel controls
  from typing controls.
- Verification layer: routing tests covered wheel direction, magnitude, bytes,
  and message count. Desktop headless probes have no iPhone software keyboard,
  and source/build checks did not assert that scrolling avoids textarea focus.
- Evidence: both reported button and gesture paths converge on the same explicit
  focus call; xterm source confirms that call focuses the textarea. No backend,
  WebSocket, tmux, or C014 coalescing behavior is involved.

## Corrective Action

- Remove terminal focus from the shared application-wheel dispatcher and App
  Scroll toggle.
- Retain focus behavior for terminal connection/reconnection, actual shortcut
  keys, Ctrl/Alt typing modifiers, and paste completion.
- Preserve wheel dispatch, coalescing, velocity, Older/Latest routing, default
  tmux history, and connection behavior unchanged.

## Preventive Controls

- Interaction guard: wheel-only controls must be focus-neutral and must not call
  xterm's textarea-focused `focus()` method.
- Regression inspection: enumerate every `focus()` caller in TerminalView and
  verify only typing/reconnect/paste paths retain it; App Scroll dispatch/toggle
  paths must contain none.
- Physical regression: dismiss the iPhone keyboard, then exercise both swipe
  directions and application-mode Older/Latest; the keyboard must remain closed
  and the terminal connected.

## Narrower Next Action

Create C015, remove only the two App Scroll focus calls, run focused/canonical
and production verification, redeploy under the owner's explicit approval, and
inspect physical keyboard, connection, and rate-limit behavior.
