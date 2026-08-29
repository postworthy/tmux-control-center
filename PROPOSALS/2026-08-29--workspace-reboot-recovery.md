# Proposal: Workspace Reboot Recovery

Date: 2026-08-29
Owner: Human Partner and AI Agent
Risk Class: T2 locally; T3 for host deployment
Related Issue/Context: The owner wants active tmux workspaces to return after a
machine reboot, with Codex and Claude conversations resumed automatically.
Roadmap Item: C021
Planned Branch: `feat/session-workspace-restore`
Expected Commit Count: 2-4

## Objective

Preserve content-free tmux workspace metadata on the Linux host and, only after
an explicit authenticated action in the app, reconstruct it after reboot while
resuming local Codex and Claude Code panes and restoring every other pane as an
ordinary shell.

## Scope

In scope:

- Periodic and graceful-stop snapshots of tmux session names, window names,
  pane working directories, pane/window ordering, layouts, and active selection.
- An allowlisted pane classification containing only `codex`, `claude`, or
  `shell`; no captured command line is persisted or replayed.
- A protected in-app Restore action with no caller-controlled target, path, or
  command; it writes one fixed request for the host helper.
- Reconstruction only after that request and only when no tmux sessions exist;
  booting or starting either service never triggers restore by itself.
- Fixed resume commands: `codex resume --last` and `claude --continue`, each
  launched in the restored pane directory and followed by a login shell if the
  tool is unavailable or exits.
- A repository-owned host daemon and systemd system-service template that run as
  the tmux owner and preserve tmux across daemon restarts.
- Atomic owner-only snapshot storage, fail-closed parsing, partial-restore
  cleanup, isolated real-tmux tests, documentation, review, and deployment to
  the existing host after explicit owner approval.

Out of scope:

- Terminal output/scrollback, prompts, environment variables, credentials,
  arbitrary command lines, process memory, or exact CPU/process continuation.
- Automatic SSH reconnection, remote Codex/Claude detection or resume, and
  automatic restart of any tool other than local Codex or Claude Code.
- Browser-supplied commands, automatic boot restore, snapshot editing UI,
  multiple hosts, cloud backup, history browsing, or restoring over an
  already-running tmux server.

## Expected Files Touched

- `PROJECT-BRIEF.md`, `SPEC.md`, `DECISIONS.md`, and roadmap/status documents
- `scripts/tmux-workspace-recovery.sh`
- `tests/tmux-workspace-recovery.test.sh`
- `deploy/systemd/tmux-mobile-workspace@.service`
- server and frontend restore status/action integration
- setup/deployment/architecture/security documentation
- Tempo goal and review records

## Acceptance Criteria

- [ ] A snapshot contains only version/timestamp, names, indices, layouts,
  directories, active flags, and the `codex`/`claude`/`shell` classification;
  it is atomically replaced with owner-only permissions.
- [ ] Restore never runs at boot or service start; one authenticated, authorized,
  CSRF-protected in-app action requests reconstruction only when tmux is empty,
  and a malformed snapshot creates nothing.
- [ ] Codex panes execute only `codex resume --last`, Claude panes execute only
  `claude --continue`, and all other captured programs—including SSH—become
  ordinary shells in their prior local directory.
- [ ] Restarting or stopping the recovery daemon does not terminate tmux or its
  sessions, and startup with a saved empty-server state remains idle until the
  app requests restore exactly once.
- [ ] Isolated real-tmux tests exercise round-trip recovery, allowlisting,
  privacy exclusions, corruption, and already-running-session protection.
- [ ] Canonical verification, host installation, service health, snapshot
  permissions, app health, and rollback evidence are current.

## Verification Plan

```bash
bash tests/tmux-workspace-recovery.test.sh
./scripts/verify.sh
systemd-analyze verify deploy/systemd/tmux-mobile-workspace@.service
systemctl status tmux-mobile-workspace@<owner>.service
```

Deployment evidence must additionally prove an isolated socket round trip,
owner-only snapshot permissions, unchanged live-session identities during
service restart, and healthy Tailscale application reachability.

## Change Review Plan

- Review Boundary: merge from `feat/session-workspace-restore` into `main`
- Planned Review Record: `REVIEWS/2026-08-29--workspace-reboot-recovery.md`
- Reviewer expectation: inspect process-launch allowlisting, snapshot privacy,
  parser safety, partial rollback, systemd cgroup behavior, existing-session
  protection, boot ordering, documentation, and deployment rollback.

## Git Plan

- Branch: `feat/session-workspace-restore`
- Planned implementation commit: `feat(tmux): restore workspaces after reboot`
- Planned review/deployment record: `docs(review): record workspace recovery rollout`

## Decomposition Plan

1. Product and recovery contract — exit when allowed metadata and launch
   boundaries are explicit — Risk T1.
2. Snapshot thin slice — exit when one isolated tmux workspace produces an
   atomic, content-free, owner-only snapshot — Risk T1.
3. Restore engine — exit when an isolated multi-pane workspace round-trips and
   only the two fixed agent resume commands launch — Risk T2.
4. App/host bridge — exit when one protected app request triggers one host
   restore, while boot and daemon restart remain idle and tmux stays alive — Risk T2.
5. Verification/review/deployment — exit when gates, rollback, live service,
   and reboot-equivalent recovery evidence pass — Risk T3 at deployment.

Thin slice: save and restore one isolated shell-only tmux session with the same
name and directory without addressing the user's default tmux server.

Intentional deferrals: exact agent-session ID association, remote recovery,
manual snapshot editing, encrypted/off-host snapshots, and arbitrary tool adapters.

## Rollback Plan

Disable and remove the host systemd service and its installed recovery script,
leaving the owner-only snapshot for optional recovery or deletion. Existing
tmux sessions remain owned by the host tmux server. Revert repository commits
and redeploy the prior application image only if tracked app artifacts changed.

## Risks and Mitigations

- Risk: boot launches an unintended or destructive command. Mitigation: never
  save/replay command lines; persist a three-value classification and map only
  two values to fixed resume commands.
- Risk: duplicate, unattended, or overwritten live sessions. Mitigation: never
  restore on boot; require a protected explicit action, restore only when tmux
  has no sessions, and never merge snapshots into a live server.
- Risk: corrupt/tampered state causes partial reconstruction. Mitigation:
  validate the full snapshot before mutation and remove only sessions created
  by a failed restore attempt.
- Risk: stopping the daemon kills restored tmux. Mitigation: systemd uses
  `KillMode=process`; tests prove daemon restart preserves the tmux server.
- Risk: agent resumes the wrong recent conversation when several exist in one
  directory. Mitigation: document directory-scoped `--last`/`--continue`
  semantics; exact ID mapping is intentionally deferred.

## Compatibility / Migration Notes

The web API gains one additive status route and one fixed restore-request route.
Compose gains one protected host-state mount. Recovery remains disabled until
the host service is installed/enabled, and existing tmux sessions take
precedence over saved state.

## Observability / Debug Notes

The daemon logs version, counts, action, and failure category only. It never
logs pane output, commands, names, paths, snapshot records, or agent transcripts.

## Approval

- Requested from: repository owner
- Approval status: approved for implementation and deployment
- Approved at: 2026-08-29 through the explicit request to build and deploy the
  agreed Codex/Claude-only recovery scope, followed by the explicit correction
  that restore must be initiated from within the app rather than at boot.
