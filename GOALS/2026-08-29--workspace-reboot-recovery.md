# Goal: Restore Tmux Workspaces After Reboot

Status: active
Owner: Human Partner and AI Agent
Risk: T2 locally; T3 host deployment
Updated: 2026-08-29
Proposal: `PROPOSALS/2026-08-29--workspace-reboot-recovery.md`
Review Boundary: merge from `feat/session-workspace-restore` into `main`

## Outcome

After a host reboot, the app offers an explicit Restore action. Only after the
owner invokes it do saved tmux sessions return with their names, window/pane
structure, layouts, and directories; local Codex and Claude panes resume their
latest directory-scoped conversations, while every other pane returns as a shell.

## Non-Goals

- No automatic boot restore, terminal-content persistence, arbitrary command
  replay, SSH reconnect, remote-agent restore, process-memory checkpoint,
  caller-controlled browser command surface, multiple hosts, merge, push, or
  publication.

## Acceptance Criteria

- [ ] AC1 — Snapshots are atomic, owner-only, content-free, and contain only
  the approved metadata schema and three-value pane classification.
  - Evidence: pending
- [ ] AC2 — An isolated workspace round-trip preserves session/window names,
  pane directories/counts, layouts, and active selections.
  - Evidence: pending
- [ ] AC3 — Only Codex and Claude panes launch fixed resume commands; SSH and
  every unknown program restore as shells, with no captured argv replay.
  - Evidence: pending
- [ ] AC4 — Boot/service start never restores; one protected in-app request can
  restore once, while corrupt snapshots create nothing, live sessions block
  restore, and failed partial restores clean up only newly created sessions.
  - Evidence: pending
- [ ] AC5 — The host service runs as the tmux owner, saves periodically and on
  stop, waits for explicit app requests, and daemon restart leaves tmux alive.
  - Evidence: pending
- [ ] AC6 — Focused/canonical verification, documentation, Change Review,
  local commits, host rollout, live app checks, and rollback evidence pass.
  - Evidence: pending

## Authority Envelope

### May Continue Without Asking

- The owner's 2026-08-29 request authorizes the scoped local T2 implementation,
  isolated tmux testing, local commits, image build, and deployment of this
  non-root recovery service to the existing tmux-mobile host.
- Reversible T0/T1 corrections inside the fixed metadata and process-launch
  boundary, plus read-only production inspection and health verification.

### Must Pause for Approval

- Touching or killing existing user tmux sessions; arbitrary/captured command
  replay; SSH or remote-agent automation; terminal-content persistence;
  secrets; public/network exposure changes; destructive snapshot deletion;
  merge, push, publication; compatibility breaks; or unclear security impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Contracts | completed | Approved schema, boundaries, risk, and rollback are durable. | Contract inspection |
| 2. Snapshot thin slice | completed | Isolated tmux metadata saves atomically with mode 0600. | Focused shell test |
| 3. Restore engine | completed | Isolated multi-pane state round-trips with fixed agent resumes. | Real isolated tmux test |
| 4. App/host bridge | completed | Protected app action triggers one restore; boot remains idle and daemon restart preserves tmux. | API/systemd integration probe |
| 5. Review and deploy | in progress | Gates pass and live service is installed, healthy, and reversible. | Canonical + live evidence |

## Progress

- 2026-08-29: owner approved automatic recovery for Codex and Claude only;
  every other tool returns as a shell in the same tmux name/directory context.
- 2026-08-29: existing deployed dirty stack was reviewed, verified, and locally
  preserved as commit `005e182` before this feature branch was created.
- 2026-08-29: owner explicitly rejected automatic boot restore. Scope changed to
  automatic saving plus an authenticated in-app Restore action.
- 2026-08-29: implemented the metadata-only snapshot/restore engine, fixed
  Codex/Claude resume classification, owner-only request bridge, Admin+CSRF app
  action, empty-inventory UI, Compose state mount, host unit, and documentation.
- 2026-08-29: focused real-tmux, server, frontend, setup, watchdog, Compose, and
  production-build verification passed; deployment review is next.

## Evidence

- `bash tests/tmux-workspace-recovery.test.sh`: real isolated tmux round-trip
  passed with one session, two windows, three panes, fixed Codex/Claude markers,
  corrupt/live-session refusal, explicit request consumption, and daemon
  restart session survival.
- .NET 10 container verification: 27 Core, 23 Infrastructure with four expected
  opt-in skips, and 48 server integration tests passed.
- Frontend typecheck, five unit suites, and production Vite build passed; setup,
  watchdog, Compose rendering, shell syntax, and `git diff --check` passed.
- The opt-in PTY-only test attempt was blocked by the SDK test image lacking
  `/usr/bin/tmux`; equivalent recovery lifecycle coverage ran against host tmux.

## Discoveries

- The container mounts only the host tmux socket directory, not arbitrary host
  project directories. If it started tmux after reboot, the tmux server would
  live in the container boundary and could not reconstruct host workspaces.
- A host-side non-root recovery service is therefore required; the web/Compose
  contracts do not need a new arbitrary process-launch surface.
- Current host tmux metadata identifies active Codex panes as `codex`, and the
  installed Codex CLI supports directory-scoped `codex resume --last`.

## Decisions

- Persist a strict metadata/classification schema rather than process argv.
- Restore only into an empty tmux server and never merge with live sessions.
- Use directory-scoped latest-session resume in v1; exact agent IDs are deferred.
- Keep reconstruction host-side and repository-owned; the browser receives only
  status plus one fixed, no-arguments restore request.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- Complete the Change Review, local feature commit, host helper installation,
  Compose rollout, and non-destructive live verification.

## Pause Conditions

- Pause at every existing-session mutation, unapproved launch expansion,
  terminal-content persistence, destructive state action, production boundary
  outside the approved host rollout, repeated unchanged failure, merge, or push.

## Outcomes

- Implementation is verified locally; deployment is pending.
