# RCA: Desktop terminal layout and history acceptance failures

Date: 2026-08-29
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `7be3911`, `08241ab`, `07b6d95`, `d7a2f73`

## Symptom

- An unmodified mouse wheel scrolls xterm's device-local buffer instead of the
  authoritative tmux session history.
- Selecting a session creates its tab and attachment, but the xterm surface can
  remain absent until the native window is manually resized.
- Maximizing/full-screening the native window can leave xterm smaller than the
  available workspace; another manual window resize is required to refit it.
- The primary terminal chrome presents session, tmux-window, and pane rows at
  once. The owner rejected this three-row hierarchy as excessive for the first
  desktop cut and requires one session-tab row.
- The persistent 260px session sidebar consumes terminal space and cannot
  collapse to a narrow icon rail.

## Reproduction

1. Launch the deployed Ubuntu Photino client and connect to the current C022
   server.
2. Select a session from the left list. Observe that the top-level tab and tmux
   attachment open while the terminal renderer may remain absent.
3. Resize the native window by dragging an edge. Observe that xterm appears and
   fits after this new layout notification.
4. Maximize or full-screen the window. Observe that xterm may retain the prior
   dimensions until another manual resize.
5. Use an unmodified mouse wheel over xterm. Observe local xterm scrolling
   rather than the server's typed tmux copy-mode history path.
6. Observe the always-visible session tab strip, tmux window strip, and pane
   strip plus the non-collapsible session sidebar.

## Root Cause

- `DesktopTerminal` calls `fit.fit()` synchronously while mounting, before the
  WebView has a reliably settled visible grid box. Its active-tab effect is
  declared before xterm initialization and schedules only one animation-frame
  attempt; at that boundary the refs or final WebKitGTK layout may not yet be
  ready. The ResizeObserver watches the terminal host, but the implementation
  does not treat a non-positive initial measurement as pending work.
- Refitting is not registered directly for native-window resize/fullscreen
  transitions, and the terminal host relies on percentage sizing inside a grid
  whose automatic topology row can change asynchronously. A manual window resize
  supplies the later observation that the initial and maximize paths lack.
- The desktop terminal never intercepts an unmodified wheel. Xterm therefore
  applies its local scrollback behavior even though the server already exposes
  a bounded, rate-limited `older`/`newer` tmux history protocol.
- The three-row hierarchy is not an accidental rendering duplication: D011 and
  the original C022 contract required simultaneous session/window/pane chrome.
  Physical acceptance changed that product decision. Tmux topology remains
  authoritative, but the owner no longer wants dedicated window/pane rows in
  the primary desktop surface.
- Sidebar collapse was absent from the approved interaction scope and has no
  state, control, or compact CSS mode.
- Earlier Xvfb runtime checks proved attachments, tab cycling, pop-outs, and
  terminal lifecycle but did not assert a non-zero initial xterm viewport,
  maximize/fullscreen refit, authoritative desktop wheel history, chrome row
  count, or collapsed-sidebar geometry. The focused frontend tests contain no
  DOM/layout harness, so the successful verification could not falsify these
  physical WebKitGTK failures.

## Corrective Action

- Route unmodified desktop wheel gestures through the existing typed tmux
  `older`/`newer` history messages. Coalesce gestures to stay within the server's
  four-operation-per-second history limiter. Keep Ctrl+wheel exclusively for
  bounded font zoom and prevent both paths from reaching xterm's local wheel
  handler.
- Replace the one-shot fit with a layout-settling scheduler invoked after xterm
  initialization, active-tab changes, host/parent observations, native window
  resize, and fullscreen changes. Make the active terminal host fill the stage
  by inset geometry and send tmux resize only after a successful fit.
- Remove the always-visible topology bar from the primary desktop layout and
  retain one top-level session-tab row. Keep tmux and the existing safe server
  topology contract authoritative; window/pane manipulation remains available
  through normal tmux terminal interaction and can return later behind a compact
  opt-in surface if the owner requests it.
- Add a collapsible sidebar whose compact state is a narrow vertical icon rail
  with explicit accessible controls to expand sessions, create a session, and
  return to server profiles.

## Preventive Controls

- Test/Guard: pure tests cover wheel modifier routing, direction, coalesced page
  bounds, and the four-per-second dispatch interval.
- Test/Guard: add layout scheduling tests for repeated settled-frame attempts
  and keep explicit resize/fullscreen listeners in source inspection.
- Physical regression: selecting a session must show a non-zero xterm viewport
  without manual resizing; maximize/fullscreen and sidebar collapse/expand must
  each refit and change tmux rows/columns.
- UI acceptance: the desktop checklist must require exactly one persistent tab
  row above the terminal and a usable icon-only collapsed sidebar.

## Resolution Evidence

- `desktopTerminalWheel.test.ts` proves direction, page bounds, invalid input,
  and a dispatch interval compatible with the server's four-operation limiter.
- `desktopTerminalLayout.test.ts` proves zero-size measurements are rejected and
  activation includes multiple delayed layout-settling refits.
- Desktop TypeScript compilation, the production Vite desktop build, all nine
  frontend suites, and the canonical repository verification gate pass.
- After explicit owner approval, image `sha256:d6dadb4f...` passed the isolated
  tmux 3.4 compatibility probe and replaced the Compose app service. It is
  healthy with zero restarts; HTTPS liveness, exact protocol-1 capabilities,
  corrected desktop asset `index-DcoKvsuW.js`, unchanged loopback/Serve routing,
  and direct-backend 426 denial pass. Initial/fullscreen geometry, physical wheel
  input, and icon-rail interaction remain explicit owner checks.
