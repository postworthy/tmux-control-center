# RCA: Desktop reconnect backoff resets on handshake

Date: 2026-08-31
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `66c7a53`

## Symptom

- Desktop terminal tabs enter `reconnecting` readily and can remain in a rapid
  reconnect loop.
- The loop repeatedly allocates and tears down server-side PTYs.

## Reproduction

1. Open several desktop sessions and encounter a terminal transport that closes
   shortly after its WebSocket handshake succeeds.
2. Observe the tab alternate through `connected` and `reconnecting` while
   retrying approximately once per second.
3. Inspect server logs and observe a new PTY for the same tab on every retry.

## Root Cause

- `DesktopTerminal` sets `retryAttempt = 0` in every WebSocket `open` handler.
  A handshake therefore erases failure history even when the connection closes
  milliseconds later. The close handler always schedules the minimum one-second
  retry rather than advancing through the existing bounded exponential backoff.
- Terminal and inventory `close` handlers also update connection state and
  schedule a retry even when their socket is no longer the current socket. An
  old close callback can therefore overwrite a newer socket's `connected` state
  with `reconnecting`; the scheduled retry then sees the healthy socket and
  returns without repairing the stale label.
- Live server evidence on 2026-08-31 showed repeated successful terminal
  handshakes followed by PTY cleanup 1-4 ms later at roughly one-second
  intervals. This directly matches the client reset path and is distinct from
  connection-capacity handling; no capacity or input-rate warning accompanied
  the loop.
- Existing reconnect tests validate only the delay function's numeric outputs.
  They do not define when an opened connection is stable enough to reset its
  failure history.

## Corrective Action

- Reset retry history only after a connection remains open for a defined
  stability interval. Short-lived handshakes retain and increment the failure
  count, allowing retries to reach the existing 30-second cap.
- Clear the stability timer on close and component cleanup.
- Ignore close callbacks from sockets that have already been superseded.

## Preventive Controls

- Test/Guard: cover short-lived versus stable connection attempt transitions in
  the pure reconnect policy tests and retain source guards for current-socket
  close handling.
- Operational evidence: server logs must no longer show a failing tab creating a
  new PTY every second indefinitely.

## Resolution Evidence

- Corrective commit `66c7a53` is deployed in image
  `tmux-mobile:desktop-stability-66c7a53` (`sha256:696d49f9...`). The container
  is healthy with zero restarts, and bounded post-rollout logs contain no rapid
  terminal handshake/PTY cleanup cycle.
- All six predeployment tmux sessions retain their exact IDs, names, window
  counts, and attachment states after the app-only replacement.
- Physical fault-injection acceptance remains pending after the owner fully
  quits and relaunches the rebuilt desktop client.
