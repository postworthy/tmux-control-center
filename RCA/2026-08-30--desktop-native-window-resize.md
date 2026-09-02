# RCA: Desktop terminal does not follow native window resize

Date: 2026-08-30
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `99b2af7`

## Symptom

- On Ubuntu, resizing the Photino window does not resize the active xterm.js
  terminal or send updated rows and columns to tmux.
- Initial activation and some explicit layout transitions can still fit, so the
  failure is specific to continued native-window resizing.

## Reproduction

1. Launch the current self-contained Photino client and open a tmux session.
2. Drag a native window edge to materially change the content area.
3. Observe that the terminal grid remains at its previous dimensions.

## Root Cause

- The corrective implementation in `DesktopTerminal.tsx` assumes WebKitGTK will
  deliver a DOM `window.resize` or `ResizeObserver` notification for every
  Photino native-window geometry change. There is no independent check that the
  terminal host's measured dimensions changed.
- Photino exposes a native `RegisterSizeChangedHandler`, confirming that native
  size changes are a distinct event boundary. The desktop page currently has no
  native resize bridge and no geometry watcher across that boundary.
- Evidence: the implementation registers only DOM observers/listeners; the
  owner reproduced the failure in the physical Ubuntu app; the focused tests
  assert only positive geometry and delayed retry constants, not continued
  geometry-change detection.

## Corrective Action

- Retain event-driven fitting, and add an active-terminal geometry watcher that
  compares measured host width and height at a bounded interval. A changed
  measurement schedules xterm fitting and the existing deduplicated tmux resize
  message regardless of which native/browser resize event was delivered.
- Add focused tests for stable versus changed geometry.

## Preventive Controls

- Test/Guard: require a changed host geometry key to trigger a fit and an
  unchanged key to remain idle.
- Process update: physical native-window drag resize remains explicit owner
  acceptance evidence; browser-only layout checks cannot close that criterion.
