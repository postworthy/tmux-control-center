# RCA: Desktop fullscreen width exceeds PTY bound

Date: 2026-08-31
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `2f76c73`, `66c7a53`

## Symptom

- Expanding tmuxctl beyond a repeatable width changes an open terminal tab from
  `connected` to `reconnecting`; it cannot remain connected while the window is
  wider than that boundary.
- Reducing the window below the boundary allows a subsequent connection to
  remain attached.
- The behavior became visible once the native maximize correction reliably
  refit xterm to the actual fullscreen geometry.

## Reproduction

1. Open `work-claude` in the rebuilt Ubuntu desktop client on the 5120×1440
   display and observe a stable connection in the reduced window.
2. Maximize the native window and observe the tab enter `reconnecting`.
3. Leave it maximized and observe short-lived retry attachments; reduce the
   window and observe the next retry remain connected.
4. Independently create a disposable tmux session and attach through the live
   protected WebSocket endpoint. Send one resize message at 500×65 and then in
   a separate attachment at 501×65. The 500-column socket remains open after
   2000 ms; the 501-column socket closes immediately with WebSocket code 1007
   and reason `Invalid terminal message`.

## Evidence

- The active display is 5120×1440 at 120 Hz. In the reduced 4664×1371 tmuxctl
  window, tmux reports the desktop client as 470×83. Expanding the remaining
  horizontal pixels at the same font/cell width crosses 500 columns.
- Live logs show the first `work-claude` PTY (`s_da2b74cad5bfd34ccf1ea271`)
  attach at 05:10:52 and disconnect 5.25 seconds later at the maximize action.
  Maximized retries then terminate after milliseconds or a few seconds and
  advance through the new bounded backoff; the retry after reducing the window
  remains attached.
- `DesktopTerminal` calls the fit addon's unrestricted `fit()` and transmits
  the resulting rows and columns. It checks only that the host has positive
  pixel dimensions.
- `LinuxPseudoTerminal.ResizeAsync` accepts only 10–500 columns and 5–300 rows.
  A 501-column request throws `ArgumentOutOfRangeException`; the WebSocket input
  parser catches that as `ArgumentException` and deliberately closes the socket
  as invalid payload, explaining both the 1007 close and absence of an
  unhandled server error.
- The isolated probe session was removed after the comparison. The production
  container remained healthy and no live user session was killed or renamed.

## Root Cause

- The browser and PTY disagree on the terminal-dimension contract. The desktop
  frontend has no upper grid bound, while the real Linux PTY adapter enforces a
  private 500×300 maximum. A sufficiently wide fitted xterm therefore emits a
  syntactically valid resize that the server reclassifies as invalid terminal
  input and closes.
- The native maximize fix did not create a transport defect. It made the fit
  path reliable enough to reach the pre-existing server limit on this 5K-wide
  display, so resize and reconnect appeared as one failure.
- The reconnect correction behaves as implemented by preventing an unbounded
  one-second loop, but it cannot make a connection stable while every retry
  repeats the unsupported fullscreen resize.
- Prior verification checked native event delivery, positive host geometry,
  refit scheduling, and reconnect timing independently. It did not test the
  fitted grid against the real PTY's upper bound. Server WebSocket tests use a
  fake PTY without the Linux adapter validation, and real PTY tests use only an
  80×24 initial size.

## Corrective Action

- Establish one explicit bounded terminal-grid contract large enough for the
  supported 5K Ubuntu and Apple Silicon display targets at the minimum font
  size, while retaining a finite resource ceiling.
- Make the desktop fitter cap its proposed xterm grid to that contract before
  resizing xterm or sending the WebSocket message, and validate the same bounds
  at the server message boundary rather than relying on an adapter exception.
- Preserve exact wide-screen fitting by raising the current private 500×300
  adapter limit to the approved bounded contract instead of merely leaving the
  right side of a 5K terminal unused.

## Preventive Controls

- Add a focused frontend regression proving a 5120-pixel/minimum-font proposal
  produces supported rows and columns, and that sent dimensions equal xterm's
  logical grid.
- Add a real-adapter or integration boundary test proving the maximum is
  accepted and the first value above it receives an intentional bounded
  response without an unexplained PTY teardown.
- Retain physical Ubuntu acceptance at both sides of the former threshold and
  require the maximized terminal to stay connected through settled refits.

## Resolution Evidence

- The local correction establishes a shared 10–2048 column by 5–1024 row
  contract across desktop fitting, server message validation, and Linux PTY
  initial/resize paths. A 5K/minimum-font-sized 1067×480 proposal is retained
  exactly; still-larger proposals clamp xterm and the transmitted dimensions
  together.
- Focused frontend checks, all twelve frontend suites, production build,
  delivery guards, 32 Core, 41 Desktop, 58 Server integration, and 26 ordinary
  Infrastructure tests pass. Six opt-in isolated Linux tests pass with tmux 3.4,
  including real `TIOCSWINSZ` acceptance at 2048×1024 and rejection at 2049.
- The canonical host command passes its shell guards but cannot start its .NET
  leg because the host has SDK 8 while `global.json` pins 10.0.300. The same
  solution passes in the repository's compiler-equipped .NET 10 image; frontend
  and Compose legs pass directly.
- Corrective commit `e43e5f5` is live as image
  `tmux-mobile:high-resolution-grid-e43e5f5` (`sha256:73fab731...`), healthy at
  zero restarts with the prior image retained as
  `tmux-mobile:pre-high-resolution-grid-20260831`. A protected disposable live
  WebSocket/real-PTY probe stays connected at 2048×65 for 2000 ms and rejects
  2049×65 with the explicit code 1007 reason `Invalid terminal dimensions`.
  All six user sessions match the predeployment snapshot. Physical fullscreen
  acceptance remains pending.
