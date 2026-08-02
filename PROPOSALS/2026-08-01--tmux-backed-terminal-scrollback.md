# Proposal: tmux-Backed Terminal Scrollback Correction

Date: 2026-08-01
Owner: Human Partner and AI Agent
Risk Class: T2
Related RCA: `RCA/2026-08-01--xterm-local-scrollback-noop.md`
Roadmap Item: C009
Planned Branch: `fix/c009-tmux-backed-scrollback`
Expected Commit Count: 2

## Objective

Make terminal swipes navigate authoritative tmux pane history through bounded
copy-mode controls, correcting the no-op xterm-local implementation.

## Scope

In scope:

- Typed terminal WebSocket messages for older, newer, and latest history.
- Server-side session resolution and fixed tmux `copy-mode`/`send-keys -X`
  argument arrays.
- One bounded operation per completed one-finger vertical gesture.
- History cleanup on Latest, terminal exit, disconnect, and component teardown.
- Frontend protocol, server integration, tmux argument, canonical, live-bundle,
  and physical-iPhone acceptance evidence.

Out of scope:

- Changing history depth, tmux configuration/key bindings, arbitrary tmux or
  shell commands, mouse-mode configuration, selection/copy redesign, or test
  input into user sessions.

## Acceptance Criteria

- [x] Drag down requests bounded older tmux history; drag up requests newer
  history while copy mode is active.
- [x] Older enters tmux copy mode; Latest cancels it and returns to live output.
- [x] History messages cannot carry arbitrary commands and do not write PTY
  input bytes.
- [x] Invalid action/page-count/session data is rejected or clamped safely.
- [x] Disconnect after app-entered history mode performs best-effort cleanup.
- [x] Focused frontend, server, and infrastructure regressions plus canonical
  verification pass.
- [x] Exact-IP live health/bundle/listener checks pass.
- [x] Older and Latest are always the first two controls inside the bottom
  shortcut bar; Latest is disabled only while history mode is inactive.
- [x] The owner confirms the gesture on a physical iPhone before final
  acceptance.
  - Evidence: on 2026-08-02 the owner reported that iPhone testing of the
    actively deployed build was successful.

## Rollback Plan

Revert C009 and rebuild the Tailscale Serve profile. This returns to the visible
Older/Latest UI from C008 but restores its known nonfunctional behavior; no data
migration exists.

## Approval

- Requested from: repository owner
- Approval status: approved by the owner's report and request to correct the
  failed behavior
- Approved at: 2026-08-01 America/Chicago
