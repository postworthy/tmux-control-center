# RCA: Desktop right click renders two context menus

Date: 2026-08-30
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `592a1fa`, `d7957fe`

## Symptom

- Right-clicking an active desktop terminal renders tmux's full context menu and
  a second tmuxctl overlay containing only horizontal and vertical split actions.
- The smaller tmuxctl menu obscures part of the more capable tmux menu and adds
  no required interaction that tmux does not already expose.

## Reproduction

1. Launch the deployed Ubuntu tmuxctl desktop client and attach to a session
   whose tmux mouse mode exposes its context menu.
2. Right-click inside xterm.js.
3. Observe tmux's terminal-rendered menu and the separate two-item tmuxctl HTML
   menu at the same pointer location, as captured in the owner's
   `Screenshot from 2026-08-30 01-21-56.png`.

## Root Cause

- `DesktopTerminal.tsx` registered a capture-phase `contextmenu` listener that
  called `preventDefault()` and opened application state through
  `onContextMenu`, while xterm's preceding mouse input still reached tmux.
- `DesktopApp.tsx` independently rendered `terminal-menu-layer` with two typed
  split actions. The two menu systems therefore owned the same gesture without
  mutual exclusion.
- The product contract said only "context menu" and did not identify tmux as
  the sole right-click menu owner. Automated checks exercised typed split
  operations but had no negative assertion forbidding tmuxctl context-menu
  interception or duplicate menu chrome.
- Evidence: the deployed screenshot shows both independently rendered menus;
  source inspection finds the capture listener and HTML overlay in the exact
  event path, while the lower terminal-rendered menu proves tmux already
  received the mouse interaction.

## Corrective Action

- Reduce tmuxctl's terminal `contextmenu` listener to `preventDefault()` only,
  removing its callback plumbing, menu state, overlay, and menu-only topology
  lookup/split calls. Default suppression prevents WebKit's browser menu while
  allowing xterm's own event handler to run because propagation is not stopped.
- Leave xterm/tmux mouse handling untouched so right click produces only the
  authoritative tmux menu.
- Keep typed topology endpoints available for future opt-in controls; removing
  this duplicate UI does not remove server capability.

## Preventive Controls

- Contract: identify the terminal-rendered tmux menu as the sole right-click
  context menu in the primary desktop terminal.
- Regression guard: require the default-only suppression hook and fail desktop
  delivery checks if callback state or `terminal-menu-layer`/`terminal-menu`
  returns.
- Acceptance: physically right-click once and confirm only the tmux menu opens
  and its existing actions remain usable.
