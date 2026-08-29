# RCA: Runtime Starvation Recurrence After Healthy Deployment

Date: 2026-08-20
Severity: High
Related Proposal(s): `PROPOSALS/2026-08-15--runtime-starvation-resilience.md`
Related Commit(s): N/A; observed in deployed image
`sha256:60d2e566dbf0cd2ccd22d75e775e0dba1341a1992ef53b2d70b722e19255c602`

## Symptom

- Sixteen hours after an initially healthy deployment, both Tailscale Serve and
  the exact backend stopped responding while Docker still reported the app
  process running with zero restarts.
- Docker marked the container unhealthy after repeated five-second liveness
  timeouts, but `restart: unless-stopped` did not act on health state.
- The first operator-requested restart could not complete because Docker did not
  receive an exit event from the terminated process.

## Reproduction

1. With host load above 100, request HTTPS and direct-backend liveness. Both
   requests connect but receive no bytes before their five/ten-second deadlines.
2. Inspect the running container: health is `unhealthy`, restart count is zero,
   memory is bounded, and Kestrel logs heartbeat stalls from two to fourteen
   minutes rather than an application exception or OOM.
3. Inspect managed threads: one `.NET Server GC` thread is runnable and
   individually affinitized to CPU 0 while the other runtime threads wait.
4. Request a container restart. The .NET leader becomes defunct, but the same
   server-GC thread remains runnable on CPU 0 and prevents the exit event.
5. Change only that dying thread's affinity from CPU 0 to CPU 31. Docker reaps
   the container immediately.
6. Start the same image. It stalls after inventory polling begins and before
   Kestrel listens. Change only the fresh app's affinity from CPUs
   `0-7,10-31` to `4-7,10-31`; HTTPS immediately returns 200 and Docker becomes
   healthy.
7. Build the corrective image under the same host load. Roslyn independently
   stalls with its server-GC thread runnable and pinned to CPU 0. A host affinity
   change is denied because the BuildKit process is root-owned; cancel the
   disposable build, apply the same supported no-affinity setting to the SDK
   build stage, and retry with a changed intervention.

## Root Cause

- The production runtime used the default server-GC behavior, which hard-
  affinitizes each GC thread to one processor. Under sustained asymmetric host
  contention, the GC thread assigned to CPU 0 could not make forward progress;
  a coordinated GC then stopped application and Kestrel progress even though
  other processors remained usable. Microsoft documents that
  `DOTNET_GCNoAffinitize=1` removes this per-CPU coupling for server GC.
- Docker health checks correctly detected the symptom, but Compose had no
  independent containment action. `restart: unless-stopped` responds to process
  exit, not to a running unhealthy state, so the wedge persisted indefinitely.
- Shutdown inherited the same GC scheduling dependency. The leader became a
  zombie while its CPU-0 GC thread remained runnable, preventing Docker from
  observing exit until that thread was moved to a usable processor.
- C020 did not introduce this failure. The same mechanism occurred in the prior
  image on 2026-08-15; deploying a new image removed the temporary incident-only
  affinity adjustment while the approved C019 preventive work remained
  unimplemented.
- The earlier deployment verification proved immediate health, compatibility,
  bind/auth boundaries, and zero restarts, but it did not run the approved
  constrained soak, inspect GC-thread affinity, or prove automatic recovery from
  a running unhealthy process. It therefore could not catch a failure requiring
  hours of asymmetric contention.

## Corrective Action

- Set the supported .NET runtime control that prevents server-GC threads from
  being hard-affinitized to individual CPUs in both build and runtime stages.
- Replace the passive liveness command with a small image-local native watchdog:
  it keeps bounded startup/steady failure counters in tmpfs, and after a
  conservative threshold terminates only the validated direct `dotnet` child.
  Docker's existing restart policy can then restore service without touching
  the host tmux server or its sessions.
- Prove the watchdog in a disposable container by inducing a nonresponsive fake
  direct child and observing a restart and healthy recovery, then inspect a
  corrected app container to prove GC threads are no longer individually
  pinned. Bind counters to process start identity so Docker restart cannot
  inherit the previous process's failure threshold.

## Preventive Controls

- Test: Compose rendering and shell syntax are canonical gates; a disposable
  restart probe must show the watchdog causes an actual restart without Docker
  socket access or elevated capabilities.
- Guard: the watchdog acts only on the sole direct child of PID 1 when its Linux
  command name is exactly `dotnet`; ambiguous or mismatched process state fails
  closed and only reports unhealthy.
- Alert: each trip emits one bounded, content-free diagnostic into Docker's
  health history; container restart count becomes the recovery signal.
- Process: production rollout retains an image rollback, isolated tmux version
  probe, exact bind/auth checks, session-survival check, GC-affinity inspection,
  and monitored post-deploy health.

Observed result: final image
`sha256:929d2c540f11ee88b2debfff20a996b16ea5fa8ef0fedfade43b7911a63676ee`
passed the forced-wedge restart proof, isolated tmux survival proof, canonical
gate, tmux 3.4 socket probe, and production boundary checks. Production is
healthy with zero restarts and all GC threads eligible across
`0-7,10-31`; rollback remains `tmux-mobile:rollback-pre-c019-20260820`.
