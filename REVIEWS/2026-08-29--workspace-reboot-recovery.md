# Review Record: Workspace Reboot Recovery

Date: 2026-08-29
Review Boundary: C021 feature branch and authorized host deployment
Merge Method: eventual `git merge --no-ff feat/session-workspace-restore`
Risk Class: T2 locally; T3 host deployment
Related Proposal: `PROPOSALS/2026-08-29--workspace-reboot-recovery.md`

## Branch and Git Conformance

- Source: `feat/session-workspace-restore`
- Target: `main`
- [x] No direct commit to `main`
- [x] Dedicated source branch matches C021
- [x] Approved metadata and process-launch scope only
- [ ] Merge and push remain outside this review

## Change Summary

- Added atomic owner-only snapshots of tmux names, topology, layout, active
  selections, working directories, and a three-value agent classification.
- Added a host-owner daemon that saves periodically and on stop, but restores
  only after consuming a fixed app-created request while tmux is empty.
- Added authenticated status and Admin+CSRF restore APIs plus an empty-inventory
  Restore action in the PWA.
- Added Compose state mounts, setup generation, systemd templates, tests, and
  operating/security documentation.

## Acceptance and Safety Review

- [x] Boot and daemon startup do not restore.
- [x] Only exact `codex` and `claude` foreground commands receive fixed resume
  behavior; all other panes reopen as shells.
- [x] Snapshot omits output, argv, environment, credentials, and SSH targets.
- [x] Restore validates the complete snapshot before mutation, refuses live
  sessions, and cleans up only sessions created by a failed attempt.
- [x] Browser request accepts no command, target, path, environment, or body.
- [x] Shared state rejects broad Linux permissions and symbolic links.
- [x] Host service uses `KillMode=process` and the normal host `/tmp` namespace,
  so restarting the helper does not terminate or hide tmux.

## Verification Evidence

- `bash tests/tmux-workspace-recovery.test.sh`: passed against a unique real
  tmux socket, including metadata/layout/CWD round-trip, fixed agent resume
  markers, corrupt/live guards, explicit request, and daemon restart survival.
- .NET 10 test image: 27 Core, 23 Infrastructure with four expected opt-in
  skips, and 48 server integration tests passed.
- Frontend: typecheck, five unit suites, and production build passed.
- First-run setup, watchdog, shell syntax, Compose rendering, and
  `git diff --check` passed.
- Opt-in PTY tests could not run in the SDK test image because it does not
  contain `/usr/bin/tmux`; the workspace recovery test independently used real
  host tmux without touching the default socket.

## Findings

- No implementation-blocking finding.
- The explicit request is polled every two seconds; snapshot capture remains at
  the configured 60-second default.
- Live default-socket restore is intentionally not exercised because it would
  require terminating the owner's current sessions. The isolated test is the
  acceptance evidence for destructive reconstruction behavior.
- System-wide installation was blocked by interactive sudo. The deployed
  owner-owned cron watchdog starts the locked daemon at reboot and restarts it
  within one minute; the installed user-systemd unit remains available for a
  desktop login manager.

## Deployment Evidence

- Feature commit: `dbd9474` on `feat/session-workspace-restore`; no merge/push.
- New image: `sha256:f48be26d7a6506714ade95056abb028dc5ac9fa94c6899c210cd58dfe2c6fcb5`.
- Rollback tag: `tmux-mobile:rollback-before-workspace-20260829` at
  `sha256:70b5dc05145a864f3b1e87a7df4b2c45e385c51dd8f01ca11f323b1ae04971db`.
- Container healthy, zero restarts, workspace mount present, and only
  `127.0.0.1:8780` is published.
- HTTPS root and liveness returned 200; anonymous recovery returned 401; direct
  backend returned 426. Authenticated live status reported enabled, idle,
  snapshot available, and no pending request.
- Owner daemon is active under the cron lock; snapshot mode is 0600. Existing
  tmux session IDs `$0`, `$2`, and `$3` were unchanged by rollout.

## Rollback

1. Disable `tmux-mobile-workspace@USER`; this stops future saves and requests
   without touching tmux sessions.
2. Restore the previous app image with the same keys/audit/socket mounts.
3. Preserve the workspace directory; no schema migration or deletion is needed.

## Decision

Approved and deployed. Merge and push are not approved.
