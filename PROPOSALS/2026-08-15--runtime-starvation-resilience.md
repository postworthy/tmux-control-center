# Proposal: Runtime Starvation Resilience

Date: 2026-08-15
Owner: Human Partner and AI Agent
Risk Class: T2 locally; T3 for production rollout
Related Issue/Context: `RCA/2026-08-15--service-starvation-under-host-contention.md`
Roadmap Item: C019
Planned Branch: `fix/c019-runtime-starvation-resilience`
Expected Commit Count: 4-6

## Objective

Keep tmux-mobile responsive, restartable, and diagnostically useful when tmux
operations block or the host is CPU-contended, without weakening terminal
isolation, killing tmux sessions, or depending on persistent CPU affinity.

## Scope

In scope:

- A deterministic isolated reproduction that combines constrained CPU, tmux
  subprocess churn, terminal connect/disconnect churn, and health probes.
- Bounded subprocess concurrency and complete success, failure, timeout,
  cancellation, output-drain, kill/reap, and descriptor-disposal semantics.
- A PTY lifecycle design in which native reads and child waits cannot consume
  the worker capacity required by Kestrel.
- Startup that reaches HTTP liveness independently of initial tmux inventory;
  readiness and inventory state report tmux degradation explicitly.
- An independent progress watchdog that converts a prolonged unrecoverable
  runtime wedge into bounded process exit so container restart policy applies.
- Content-free operational signals and tests covering descriptor counts,
  pending operations, PTYs, startup, liveness, shutdown, and recovery.
- Documentation, canonical verification, Change Review, rollback, and a
  separately approved production canary and soak.

Out of scope:

- Killing or reprioritizing fuzzers, Tailscale, DNS, VMs, or other host jobs.
- Persistent host CPU affinity as the solution, orchestration migration,
  Kubernetes, public ingress, or a general job scheduler.
- Changing tmux session semantics, exposing raw commands, weakening auth or
  exact-IP binding, or promising availability during kernel/host failure.
- Production deployment, service-manager changes, or destructive testing
  against the owner's default tmux server without separate T3 approval.

## Expected Files Touched

- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `src/TmuxMobile.Infrastructure/ProcessRunner.cs`
- `src/TmuxMobile.Infrastructure/LinuxPseudoTerminal.cs`
- `src/TmuxMobile.Infrastructure/native/tmux_mobile_pty.c`
- `src/TmuxMobile.Infrastructure/Inventory.cs`
- `src/TmuxMobile.Server/Program.cs`
- focused infrastructure/server tests and a constrained-resource harness
- `docs/architecture.md`, `docs/configuration.md`, and deployment documentation
- Tempo RCA, goal, evidence, and review records

## Acceptance Criteria

- [ ] A checked-in isolated harness reproduces the baseline starvation without
  touching the default tmux socket and passes after correction under the same
  CPU and churn profile.
- [ ] During a 60-minute constrained soak, `/health/live` has no failed probe
  and a one-second maximum response budget; pending subprocesses, PTYs, threads,
  children, and descriptors return to a documented bounded steady state.
- [ ] Subprocess success, large bounded output, nonzero exit, timeout,
  cancellation, and caller abandonment leave no child, zombie, pipe, or
  unfinished drain task and never invoke a shell.
- [ ] Repeated real-PTY attach/disconnect churn preserves the isolated tmux
  session, reaps every attach client, and leaves Kestrel responsive.
- [ ] Kestrel reaches liveness while tmux inventory is deliberately blocked;
  readiness reports degradation, inventory remains explicitly stale, and
  recovery occurs without restarting the application.
- [ ] Under induced non-progress, the independent watchdog produces a bounded
  diagnostic and exits within the documented interval; the container restarts
  and returns healthy without terminating the underlying tmux session.
- [ ] Graceful stop completes inside the Compose stop grace period under the
  constrained profile with no zombie or Docker `did not receive an exit event`.
- [ ] Existing API/WebSocket/security/tmux compatibility contracts, canonical
  verification, documentation, Change Review, rollback, and approval-gated
  canary evidence all pass.

## Verification Plan

Commands:

```bash
dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj
./scripts/verify.sh
```

New focused harness, final name fixed during Unit 1:

```bash
./scripts/verify-runtime-resilience.sh --tmux-socket isolated --duration 60m
```

Pass means all criteria have captured command output, the constrained baseline
fails for the expected reason, the corrected build passes the identical profile,
resource curves remain bounded, and canonical verification exits zero.

## Change Review Plan

- Review Boundary: merge from `fix/c019-runtime-starvation-resilience` into `main`
- Planned Review Record: `REVIEWS/2026-08-15--runtime-starvation-resilience.md`
- Reviewer expectation: inspect native/process lifecycle, cancellation and reap
  guarantees, watchdog independence, health semantics, isolated-test safety,
  compatibility, rollback, and criterion evidence.

## Git Plan

- Branch command after the current C018 worktree is safely integrated or
  otherwise preserved: `git switch -c fix/c019-runtime-starvation-resilience`
- Commit subject pattern: `fix(runtime): <bounded resilience unit>`
- Required trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C019`
  - `Proposal: PROPOSALS/2026-08-15--runtime-starvation-resilience.md`
- Planned merge method: `git merge --no-ff fix/c019-runtime-starvation-resilience`

## Decomposition Plan

1. Reproduction and measurement thin slice — create an isolated constrained
   harness and record baseline health, latency, descriptor, thread, child, and
   shutdown curves — Risk T1 — Exit: the deployed behavior fails
   deterministically without using the default tmux server.
2. Subprocess lifecycle — bound concurrency and make drain/timeout/cancel/reap
   ownership explicit — Risk T2 — Exit: the subprocess matrix passes with zero
   leaked resources.
3. PTY lifecycle isolation — select and implement the smallest proven native or
   dedicated-thread design that prevents PTY reads/waits from starving Kestrel
   — Risk T2 — Exit: real-PTY churn passes under the constrained profile.
4. Startup and health semantics — make initial inventory asynchronous from HTTP
   startup and represent stale/degraded inventory accurately — Risk T1 — Exit:
   liveness stays responsive while tmux is blocked and readiness recovers.
5. Failure containment and shutdown — add an independently scheduled watchdog
   plus bounded stop/reap behavior — Risk T2 — Exit: induced wedge exits,
   restarts, and graceful stop completes without session loss or zombies.
6. Soak, compatibility, docs, and review — run 60-minute constrained soak,
   canonical verification, clean-image compatibility, and Change Review — Risk
   T1 locally; production canary/soak is a separate T3 boundary.

Thin slice milestone:

- Unit 1 converts the incident from a one-off observation into a deterministic,
  safe, measurable failure using only a unique `tmux -L` socket.

Dependencies and unknowns:

- The managed stack could not be captured in production; Unit 1 must confirm
  which pipe/PTY operations consume worker capacity before architecture changes.
- PTY masters have platform-specific async behavior; choose native nonblocking
  polling, dedicated threads, or another mechanism only from measured evidence.
- A watchdog must be independent of the starved scheduler it observes and must
  avoid false exits during expected GC or host suspension.
- The current dirty C018 worktree must be preserved before creating the C019
  feature branch.

Intentional deferrals:

- Host-wide workload policy and investigation of abnormal `tailscaled` or DNS
  CPU use are separate operational work.
- Multi-host supervision and high-availability deployment remain out of scope.

## Rollback Plan

1. Revert C019 commits or restore the preserved pre-C019 image.
2. Remove only new resilience configuration and restore prior health/readiness
   semantics; do not remove keys, audits, or tmux socket state.
3. Validate rollback with canonical verification, isolated tmux compatibility,
   exact-IP binding, HTTPS liveness, direct-backend denial, and session survival.
4. If the watchdog causes false exits, disable it through its fail-closed
   validated configuration or restore the prior image before further rollout.

## Risks and Mitigations

- Risk: native PTY changes corrupt output or leak children. Mitigation: isolate
  ownership, test binary output/cancellation/EIO, and use disposable tmux sockets.
- Risk: watchdog false positives interrupt terminals. Mitigation: independent
  monotonic thresholds, startup/suspend allowance, explicit diagnostics, staged
  canary, and preserved rollback; tmux sessions must survive app exit.
- Risk: new concurrency limits make inventory stale. Mitigation: bounded queue,
  explicit stale state, readiness signal, and recovery tests.
- Risk: stress testing affects real sessions or host workloads. Mitigation:
  unique sockets, disposable containers, explicit resource limits, and no
  default-server commands.

## Compatibility / Migration Notes

- No HTTP, WebSocket, authentication, session-ID, or persistent-data migration
  is intended. Health/readiness response meaning may become more explicit but
  existing endpoint paths remain stable.
- Existing tmux 3.4 host/container compatibility remains mandatory.
- New resilience settings require safe defaults and startup validation; older
  configuration continues to run without edits.

## Observability / Debug Notes

- Add bounded structured events for subprocess duration/result, outstanding
  operation high-water marks, PTY start/reap, inventory stale/recovered,
  watchdog trip, startup duration, and shutdown completion.
- Never log command output, terminal content, secrets, raw tmux targets, or
  unbounded identifiers.
- Detect failure through liveness latency, readiness degradation, descriptor and
  child high-water marks, watchdog exits, and container restart count.

## Approval

- Requested from: repository owner
- Approval status: approved for planning and scoped local engineering; T3
  production rollout remains separately approval-gated.
- Approved at: 2026-08-15 through the owner's request to turn the incident into
  an actionable goal and prevent recurrence through engineering and design.
