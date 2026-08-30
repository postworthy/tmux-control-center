# RCA: Desktop terminal capacity blocks mobile attachments

Date: 2026-08-30
Severity: High
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `dcb5ff9`

## Symptom

- The authenticated iPhone PWA still lists sessions, but opening any session
  terminal never connects and immediately offers Reconnect.
- The failure appeared while the desktop client retained multiple live session
  tabs.

## Reproduction

1. Leave two desktop session tabs attached as the single configured owner.
2. Authenticate the mobile PWA and confirm that inventory/session cards load.
3. Open any mobile terminal.
4. Observe that the terminal WebSocket closes before `terminal.connect` is
   audited and the PWA displays its reconnect state.
5. Inspect the live container: two `tmux attach-session` clients are children of
   the server, while the effective defaults allow only two terminal connections
   per identity.

## Root Cause

- The desktop contract changed one owner's normal concurrency from one mobile
  attachment to multiple persistent desktop attachments plus mobile, but the
  pre-desktop `Security:MaxTerminalConnectionsPerUser` default remained `2`.
  Two open desktop tabs therefore consume every owner lease.
- `WebSocketHandlers.TerminalAsync` acquires the terminal lease before resolving
  or accepting the WebSocket. When the lease is unavailable it returns HTTP 429
  without an audit record or application log, so the browser exposes only a
  generic WebSocket close and the PWA can report only Reconnect.
- Earlier verification proved two desktop tabs and independently proved mobile
  compatibility, but never held the desktop tabs open while attaching from the
  PWA. The two-client desktop test exactly filled the old limit instead of
  testing the new cross-client concurrency contract.
- Evidence: at `2026-08-30T14:11:54Z` the live audit records successful owner
  login and inventory connection but no terminal attempt; `docker top` shows two
  live server-owned tmux clients; checked-in and effective server settings are
  global `4`, per-owner `2`; the rejection branch precedes terminal audit and
  PTY startup.

## Corrective Action

- Raise the bounded default global and per-owner terminal capacity to ten. This
  matches the existing validated per-owner maximum and supports several
  persistent desktop tabs plus a mobile attachment without making connections
  unbounded.
- Audit and warn when a terminal connection is rejected for capacity so future
  reconnect-only reports identify the server-side cause.
- Preserve the existing lease cleanup, idle timeout, process limit, and input
  rate limits.

## Preventive Controls

- Add a regression test that acquires ten same-owner leases, proves an eleventh
  is rejected, and proves disposal restores capacity.
- Update configuration documentation to state that the default is shared by
  persistent desktop tabs and mobile terminals.
- Add owner acceptance with at least two live desktop tabs while a mobile PWA
  terminal attaches and detaches successfully.
