# RCA: Desktop title-bar maximize misses terminal refit

Date: 2026-08-31
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `2f76c73`

## Symptom

- Double-clicking the Ubuntu native title bar maximizes the Photino window, but
  the active xterm grid retains its prior dimensions.
- Drag-resize and explicit browser fullscreen handling do not prove this native
  window-state transition.

## Reproduction

1. Launch the Ubuntu Photino client and open a session.
2. Double-click the native title bar to maximize the window.
3. Observe that the terminal rows and columns do not refit to the maximized
   content area.

## Root Cause

- `DesktopTerminal` reacts to DOM resize, visual-viewport resize, browser
  fullscreen, `ResizeObserver`, and measured-host polling. The native shell does
  not forward Photino's distinct maximize, restore, or native size callbacks to
  the page, so WebKitGTK can miss the transition that should invalidate and
  settle the terminal layout.
- Photino.NET 4.0.16 exposes `RegisterSizeChangedHandler`,
  `RegisterMaximizedHandler`, and `RegisterRestoredHandler`; none are registered
  by `BuildWindow`.
- Evidence: owner reproduction on Ubuntu; source inspection of `Program.cs` and
  `DesktopTerminal.tsx`; prior resize tests cover only DOM geometry and polling,
  not the native-to-web event boundary.

## Corrective Action

- Forward native size, maximize, and restore callbacks through Photino's web
  message bridge.
- Convert that message once at the desktop app boundary into a refit event that
  every mounted terminal can handle with its existing settled-fit scheduler.

## Preventive Controls

- Test/Guard: require all three Photino native geometry handlers, the native
  message receiver, and the terminal refit listener in the desktop delivery
  contract.
- Process update: retain physical Ubuntu title-bar maximize and restore as
  acceptance evidence separate from drag resize and browser fullscreen.

## Resolution Evidence

- Corrective commit `2f76c73` is deployed in image
  `tmux-mobile:desktop-stability-66c7a53` (`sha256:696d49f9...`); the served
  desktop bundle contains the native geometry message and refit event markers.
- The self-contained Ubuntu client was rebuilt in place and its managed payload
  contains the native geometry message while the native payload contains the
  Photino maximize handler registration. The installed launcher points to this
  artifact and it has no missing native libraries.
- Physical double-click maximize and restore acceptance remains pending after a
  full quit/relaunch of the client process that predates this rebuild.
- Follow-up physical acceptance proves the native event now reaches the fitter,
  but a 5K fullscreen fit crosses the server's separate 500-column PTY limit and
  closes the terminal socket. The linked transport failure and superseding next
  action are documented in
  `RCA/2026-08-31--desktop-fullscreen-width-exceeds-pty-bound.md`.
