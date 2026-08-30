# RCA: Desktop pop-out remains on compatibility check

Date: 2026-08-30
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `07b6d95`, `8741bf4`

## Symptom

- Selecting **Open in a new window** creates a native window, but that window
  remains on “Checking server compatibility” and never renders the requested
  tmux session.

## Reproduction

1. Launch the deployed Ubuntu desktop client and open a session tab.
2. Select that tab's **Open in a new window** action.
3. Observe a second Photino window containing only the compatibility progress
   page for `ubuntu-box-1.monster-ionian.ts.net`.

## Root Cause

- The original pop-out implementation calls `childWindow.WaitForClose()` from
  the parent window's web-message callback. That blocking call is what creates
  and pumps the child native window.
- Capability negotiation was added later and changed `child.Connect(...)` into
  an asynchronous probe whose successful continuation calls
  `childWindow.Invoke(...)` to replace the progress page with `/desktop/`.
  Because the parent callback is synchronously nested inside
  `WaitForClose()`, the continuation cannot complete the expected child-window
  transition on the Ubuntu runtime. The already-rendered progress document is
  therefore the last visible state.
- Evidence: `git blame` shows the blocking child lifecycle originated in
  `07b6d95`, while asynchronous capability navigation was introduced afterward
  in `8741bf4`; current source combines both paths. The owner observed the exact
  intermediate document produced immediately before the async probe.
- The earlier runtime evidence predates capability negotiation, and capability
  tests exercise the probe and URI independently rather than opening a real
  post-negotiation child window. They could not detect the lifecycle
  composition regression.

## Corrective Action

- Treat a pop-out request received from an already-ready desktop page as a
  continuation of that controller's successfully negotiated server connection.
  Initialize the child with its cache-busted, session-deep-linked desktop URI
  before entering `WaitForClose()` instead of starting a second asynchronous
  capability transition inside the nested child lifecycle.
- Preserve strict opaque session validation and the existing same-origin,
  authentication, cache-busting, and child-close attachment cleanup boundaries.

## Preventive Controls

- Add a unit-level navigation contract proving a known-compatible pop-out gets
  a cache-busted session URI without re-entering the compatibility progress
  state.
- Add a native runtime check that opens a pop-out after compatibility succeeds
  and waits for its `desktopReady` message rather than counting window creation
  alone as success.
