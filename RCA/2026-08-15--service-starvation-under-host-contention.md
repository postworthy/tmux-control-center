# RCA: Service Starvation Under Host Contention

Date: 2026-08-15
Severity: High
Related Proposal(s): `PROPOSALS/2026-08-15--runtime-starvation-resilience.md`
Related Commit(s): N/A; observed in deployed image `sha256:b1e0021...`

## Symptom

- The Tailscale Serve URL and direct backend stopped responding while Docker
  continued to report the application container as running.
- Docker accumulated 64 consecutive five-second liveness failures and marked
  the container unhealthy, but `restart: unless-stopped` did not restart the
  still-running process.
- A requested restart initially failed because Docker terminated the .NET
  process but could not reap its final server-GC thread.

## Reproduction

1. Inspect the deployed container after the owner reports the service offline.
2. Observe a running but unhealthy container with zero restarts and repeated
   Kestrel heartbeat delays from 17 seconds to more than two minutes.
3. Request `/health/live` through both the exact backend and Tailscale Serve;
   both time out while the exact listener and Serve route remain present.
4. Inspect TCP state and observe a listen backlog of 67 plus accepted requests
   whose bytes remain unread.
5. Inspect the application process and observe 519 file descriptors, including
   240 redirected pipes, approximately 120 unfinished subprocess output reads.
6. Restart the container. The .NET leader becomes defunct while one server-GC
   thread remains runnable on saturated CPU 0 with `SIGKILL` pending, preventing
   Docker from receiving the exit event.
7. Move that dying thread to CPU 31; it is immediately reaped. Start a fresh
   container and observe startup stall after inventory polling begins but before
   Kestrel listens. Move the fresh app process away from CPUs 0-3; Kestrel starts,
   container health becomes healthy, and HTTPS liveness returns 200.

## Root Cause

- The application permits blocking tmux subprocess, redirected-pipe, PTY read,
  and `waitpid` work to depend on the same .NET worker capacity that services
  Kestrel and host startup. Under severe host contention, completions stopped
  making forward progress, unfinished pipe reads accumulated, and the HTTP
  server eventually stopped accepting and consuming requests.
- Startup amplification made recovery fragile: the initial inventory refresh
  began before Kestrel reached its listening state, so the same stalled tmux
  output path could prevent a fresh process from becoming live.
- Container supervision did not contain the failure. Docker's
  `unless-stopped` policy reacts to process exit, not an unhealthy running
  process, while shutdown itself depended on a server-GC thread scheduled on a
  saturated CPU.
- Host contention was the trigger, not a sufficient root cause by itself. Load
  averaged approximately 36; many fuzz workers were concentrated on CPUs 0-3,
  `tailscaled` consumed approximately one CPU, and `systemd-resolved` was also
  abnormally busy. Moving only the app away from CPUs 0-3 restored forward
  progress without stopping those workloads.
- Verification missed the defect because ordinary tests use an unconstrained
  host and fake or short-lived PTYs. They prove functional cleanup, not bounded
  forward progress during CPU contention, repeated reconnects, subprocess
  churn, startup, or shutdown.
- Evidence limitation: container ptrace restrictions prevented a managed stack
  dump. The causal conclusion is supported by logs, TCP queues, descriptor
  counts, process/thread state, the controlled affinity change, and immediate
  recovery, but exact managed frames were not captured.

## Corrective Action

- Reproduce the failure deterministically with an isolated tmux socket,
  constrained CPU, subprocess churn, and terminal connect/disconnect churn.
- Isolate blocking tmux/PTY/process lifecycle work from Kestrel's shared worker
  capacity, bound concurrency, and guarantee output drain, cancellation, kill,
  reap, and descriptor disposal on every path.
- Decouple Kestrel startup and liveness from the first tmux inventory refresh;
  expose tmux degradation through readiness and stale-inventory state.
- Add an independent fail-fast containment path so a non-progressing app exits
  within a bounded interval and the existing restart policy can recover it.
- Add bounded operational signals for subprocess backlog, PTY lifecycle,
  thread-pool starvation, startup delay, and health transitions.

## Preventive Controls

- Regression: a constrained-resource soak must keep liveness responsive, keep
  descriptors and pending operations bounded, and complete startup/shutdown.
- Guard: blocking native waits and PTY reads may not consume Kestrel worker
  capacity or prevent host startup.
- Alert: unhealthy state, prolonged subprocess duration, descriptor growth, or
  fail-fast recovery must produce bounded content-free diagnostics.
- Process: production rollout requires a disposable-session canary, preserved
  rollback image, restart proof, and an approval-gated monitored soak.
