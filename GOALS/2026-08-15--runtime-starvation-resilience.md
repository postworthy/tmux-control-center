# Goal: Runtime Starvation Cannot Wedge the Service

Status: paused
Owner: Human Partner and AI Agent
Risk: T2 locally; T3 production boundary
Updated: 2026-08-15
Proposal: `PROPOSALS/2026-08-15--runtime-starvation-resilience.md`
Review Boundary: merge from `fix/c019-runtime-starvation-resilience` into `main`

## Outcome

Tmux Mobile remains live and restartable during severe CPU contention, tmux
subprocess delays, and repeated terminal churn; it contains unrecoverable
non-progress by exiting cleanly for supervised restart while preserving the
underlying tmux sessions and emitting bounded diagnostics.

## Non-Goals

- No host-wide workload management, persistent CPU affinity, orchestration
  migration, public ingress, arbitrary command capability, or default tmux
  server use in stress tests.
- No claim of availability during kernel failure, host suspension, power loss,
  or complete resource denial.
- No production deployment, service-manager mutation, merge, push, or
  publication without its separate approval boundary.

## Acceptance Criteria

- [ ] AC1 — A safe isolated harness deterministically reproduces the baseline
  starvation under constrained CPU and tmux/PTY churn, then passes unchanged
  against the corrected build.
  - Evidence: pending baseline/corrected harness reports using a unique socket.
- [ ] AC2 — Subprocess operations have bounded concurrency and complete drain,
  cancel, timeout, kill, reap, and disposal behavior with no unfinished pipe,
  child, zombie, or descriptor growth across the full outcome matrix.
  - Evidence: pending focused tests plus process/descriptor high-water report.
- [ ] AC3 — Repeated real-PTY attach/disconnect churn cannot consume Kestrel's
  required worker capacity, loses no terminal byte ordering, reaps every attach
  client, and leaves the isolated tmux session alive.
  - Evidence: pending real-PTY constrained-churn report.
- [ ] AC4 — HTTP liveness reaches and remains responsive while initial or later
  tmux inventory is blocked; readiness and inventory explicitly degrade and
  recover without an application restart.
  - Evidence: pending blocked-tmux startup/recovery integration report.
- [x] AC5 — A scheduler-independent progress watchdog exits an unrecoverably
  wedged app within its bounded threshold, container restart returns healthy,
  and the underlying isolated tmux session survives.
  - Evidence: corrected image `sha256:929d2c540f11...` reached healthy in a
    disposable container using socket `tmux-mobile-c019-watchdog-proof`;
    `SIGSTOP` of only the app child produced two steady failures, one Docker
    restart, healthy recovery with reset counters, and the isolated
    `watchdog-proof` tmux session remained present.
- [ ] AC6 — Graceful shutdown completes inside the configured stop grace period
  under contention with no zombie and no Docker missing-exit-event failure.
  - Evidence: pending constrained stop/restart report.
- [ ] AC7 — A 60-minute constrained soak records zero liveness failure, at most
  one-second liveness responses, bounded operation/resource curves, and clean
  startup/shutdown; canonical verification and clean-image compatibility pass.
  - Evidence: pending soak, `./scripts/verify.sh`, build, and tmux probe results.
- [ ] AC8 — Architecture, configuration, operations, observability, rollback,
  security compatibility, and Change Review agree; an approval-gated canary
  validates the live boundary before broader acceptance.
  - Evidence: pending docs, Review Record, rollback proof, and T3 canary record.

## Authority Envelope

### May Continue Without Asking

- The owner's 2026-08-15 request approves this scoped local T2 resilience work:
  isolated reproduction, subprocess/PTY lifecycle correction, startup and health
  decoupling, independent failure containment, tests, documentation, and review.
- Local reversible T0/T1 diagnostics and corrections inside this outcome.
- Disposable containers and uniquely named `tmux -L` servers that cannot address
  the owner's default tmux socket or sessions.

### Must Pause for Approval

- Production deployment/canary, live service restart, persistent CPU or host
  workload changes, default tmux server interaction during destructive/stress
  testing, architecture expansion beyond this proposal, compatibility break,
  merge, push, publication, or unclear security/privacy impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Reproduction thin slice | pending | Baseline fails deterministically and safely with measured cause. | Isolated constrained harness |
| 2. Subprocess lifecycle | pending | Every outcome drains, disposes, and reaps within bounds. | Focused matrix and resource counts |
| 3. PTY isolation | pending | Real PTY churn preserves Kestrel capacity and tmux session. | Constrained real-PTY test |
| 4. Startup/health | pending | Liveness is independent; readiness/staleness degrade and recover. | Blocked-tmux integration test |
| 5. Containment/shutdown | in progress | Watchdog recovery and graceful stop are bounded and zombie-free. | Watchdog recovery passed; constrained graceful stop pending |
| 6. Soak/docs/review | pending | AC7/AC8 and rollback evidence are review-ready. | 60-minute soak and canonical gate |
| 7. Production canary | pending | Approved canary remains healthy and rollback is proven. | T3 live evidence |

## Progress

- 2026-08-15: owner reported the deployed service offline. Read-only evidence
  confirmed a running/unhealthy process, Kestrel heartbeat starvation, backed-up
  TCP accepts, 240 retained pipe descriptors, and failed health checks.
- 2026-08-15: requested restart exposed an unreaped GC thread with `SIGKILL`
  pending on saturated CPU 0. Moving only the dying thread to CPU 31 allowed
  reap; moving the fresh app away from CPUs 0-3 allowed startup and health.
- 2026-08-15: owner requested an actionable prevention goal based on good
  engineering and design. RCA, proposal, and this living goal now capture the
  controlled scope and evidence requirements; implementation has not begun.
- 2026-08-20: the owner reported the service offline again and explicitly
  authorized restart, RCA, corrective implementation, and production repair.
  The deployed C020 image remained running but unhealthy with zero restarts;
  direct and Serve liveness timed out, health checks exceeded five seconds,
  Kestrel reported heartbeat stalls up to fourteen minutes, host load exceeded
  100, and a server-GC thread was observed runnable on CPU 0. C019 execution is
  now active; failure evidence was preserved before restart.
- 2026-08-20: service recovery reproduced both prior stages. Docker could not
  reap a defunct leader until its CPU-0 GC thread moved to CPU 31; the fresh app
  did not listen until its threads moved off CPUs 0-3. HTTPS then returned 200,
  Docker became healthy, and all host tmux sessions remained present.
- 2026-08-20: the first corrective build independently stalled in Roslyn with a
  runnable server-GC thread pinned to CPU 0. Host affinity mutation was denied
  for the root-owned BuildKit process, so the disposable build was canceled and
  the no-affinity runtime control was extended to the SDK stage before retry.
- 2026-08-20: the corrected build completed in six seconds at the previously
  stalled publish step. The isolated tmux 3.4 probe and image runtime contract
  passed. A disposable real-app test reached healthy, was deliberately stopped
  with `SIGSTOP`, and the independent watchdog caused Docker restart after two
  steady failures without a tmux or Docker socket mount.
- 2026-08-20: the first canonical run passed Core, Infrastructure, and 40/44
  server tests; four server hosts failed before test logic because the host
  user's 128 inotify-instance limit was already exhausted. The changed retry
  disables configuration reload for immutable test hosts rather than changing
  the machine limit.
- 2026-08-20: the first focused retry omitted the repository-local CLI home and
  the managed filesystem rejected a .NET sentinel under `/home/landon/.dotnet`;
  the retry restores the same local CLI/package paths used by `verify.sh`.
- 2026-08-20: process-identity reset protection was added after the first
  disposable restart proof showed that tmpfs can outlive the app process. The
  final proof returned healthy after exactly one restart and preserved its
  uniquely named isolated tmux session.
- 2026-08-20: final canonical verification passed 27 Core, 23 Infrastructure
  with four expected opt-in skips, 44 server integration tests, and five
  frontend suites. Image `sha256:929d2c540f11...` deployed healthy with zero
  restarts, clean watchdog state, and every server-GC thread eligible on the
  full container CPU set instead of one processor.

## Evidence

- Incident evidence is recorded in
  `RCA/2026-08-15--service-starvation-under-host-contention.md`.
- Recovery evidence: fresh process started at `2026-08-15T16:25:49Z`, Docker
  became healthy, HTTPS `/health/live` returned 200, direct backend root retained
  426 denial, and all 18 host tmux sessions remained present.
- Planning validation: `git diff --check` passed; this repository has no goal
  validator, so manual template validation confirmed every required section,
  one approved goal, zero active goals, bounded retries, and exactly one next
  action.
- Recurrence RCA:
  `RCA/2026-08-20--runtime-starvation-recurrence.md` records the repeated live,
  shutdown, startup, and build signatures plus the verification gap.
- Corrected build/runtime: image `sha256:929d2c540f11...`; SDK publish completed
  in six seconds under the load that wedged the affinitized compiler; production
  GC threads each report `Cpus_allowed_list: 0-7,10-31`.
- Production gates: HTTPS root/liveness `200`, anonymous API `401`, direct
  backend `426`, exact listener `100.85.13.102:8780`, unchanged Serve mapping,
  all host tmux sessions preserved, and clean bounded startup logs.

## Discoveries

- Docker `restart: unless-stopped` does not react to a running unhealthy
  container; failure containment must produce process exit or use a separately
  supervised mechanism.
- The initial inventory path can delay Kestrel listening, making the same
  starvation defect affect both steady state and recovery startup.
- Production ptrace restrictions prevented managed stack capture; the controlled
  harness must resolve exact managed ownership before selecting the PTY design.
- The current C018 worktree is dirty and must be preserved before a C019 branch
  is created.

## Decisions

- Treat host contention as an expected operating condition to tolerate, not as
  a reason to require stopping unrelated workloads.
- Use persistent CPU affinity only as incident recovery evidence, never as the
  product fix.
- Keep liveness independent of tmux; express tmux failure through readiness and
  explicit stale inventory.
- Require the watchdog to be scheduler-independent and session-preserving.
- Prove the baseline and corrected build with the same isolated workload before
  implementation is considered effective.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: resolved; the canonical retry disabled file reload only for
  immutable test hosts and passed without changing host inotify limits.

## Next Action

- Run the checked-in 60-minute constrained soak and constrained graceful-stop
  proof for the remaining C019 criteria, then create the C019 Change Review;
  commit, merge, and push remain separate owner-controlled boundaries.

## Pause Conditions

- Pause before any production effect, live restart, host workload/affinity
  mutation, default tmux server access in stress/destructive tests, unapproved
  architecture expansion, compatibility break, merge/push/publication, or the
  third unchanged failure.
- Pause if the harness cannot prove isolation from the owner's tmux socket or if
  watchdog evidence shows a plausible false-positive shutdown path.

## Outcomes

- The exact recurrence is contained and the corrected production image is
  healthy: per-CPU GC pinning is disabled and prolonged unhealth now forces a
  session-preserving supervised restart. Broader subprocess/PTY bounds,
  constrained graceful stop, the 60-minute soak, Change Review, and git
  integration remain pending under C019.
