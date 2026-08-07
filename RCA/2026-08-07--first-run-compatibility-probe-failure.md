# RCA: First-Run Compatibility Probe Failed Before Socket Query

Date: 2026-08-07
Severity: Medium
Related Proposal: `PROPOSALS/2026-08-07--first-run-install-skill.md`
Related Commit: N/A (uncommitted C016 verification)

## Symptom

- The host-matched C016 image built successfully, but the first real
  `probe-tmux` attempt could not execute `/usr/bin/tmux` because
  `libevent_core-2.1.so.7` was absent.
- The EXIT cleanup handler then raised `cleanup_needed: unbound variable`, so
  the uniquely named isolated probe server remained until explicit cleanup.
- No default tmux socket, long-lived Compose service, Serve mapping, or running
  deployment was addressed.

## Reproduction

1. Build `tmux-mobile:c016-probe` with `TMUX_VERSION=3.4`; the build-stage
   assertion `/opt/tmux/bin/tmux -V = tmux 3.4` passes.
2. Run `./scripts/first-run-setup.sh probe-tmux` against that image.
3. Observe the dynamic loader fail before the version check, followed by the
   cleanup trap's unbound-local error.
4. Run the image's loader inspection and observe
   `libevent_core-2.1.so.7 => not found`, while the installed aggregate package
   provides `libevent-2.1.so.7` instead.
5. Inspect `/tmp/tmux-$(id -u)` and observe only the unique failed-probe socket
   `tmux-mobile-probe-3571298-e2727ed1` in the C016 namespace.

## Root Cause

- Packaging root cause: the runtime installed `libevent-2.1-7t64`, but tmux's
  configure result linked the client to the split `libevent_core` shared object,
  which Ubuntu Noble packages separately as `libevent-core-2.1-7t64`. The
  build-stage version assertion ran where all development libraries existed, so
  it did not validate the final runtime dependency closure.
- Cleanup root cause: the EXIT trap referenced variables declared `local` inside
  `probe_tmux`. Bash ran the EXIT trap after function scope was unwound by
  `set -e`; under `set -u`, the former local `cleanup_needed` no longer existed.
- Verification gap: fake probe tests modeled Docker output and successful
  cleanup, but did not exercise an early Docker failure or final-image dynamic
  linkage. The real isolated probe correctly prevented a long-lived start, but
  cleanup itself lacked a failure-path regression.

## Corrective Action

- Install the exact runtime package `libevent-core-2.1-7t64` and assert in the
  final runtime stage that `tmux -V` matches `TMUX_VERSION`.
- Move probe cleanup state to script-global variables (or pass literal captured
  values) so EXIT cleanup remains valid after function unwinding.
- Extend focused tests with a Docker failure path that still records isolated
  cleanup, and inspect final-image `ldd` output for no unresolved libraries.
- After a successful `kill-server`, remove the exact uniquely generated socket
  inode because real verification showed tmux can leave it stale even though no
  Unix listener or server process remains.
- Explicitly stop the leftover uniquely named probe server before retrying.

## Preventive Controls

- Test/Guard: make final-stage `tmux -V` part of the Docker build, not only the
  build stage.
- Test: simulate a failed one-off container and require exactly one matching
  isolated cleanup record.
- Guard: remove only `/tmp/tmux-<current-uid>/tmux-mobile-probe-<random>` after
  its matching `kill-server` succeeds, then require no probe sockets remain.
- Review: require both final-image dependency inspection and a real isolated
  socket query before C016 acceptance; version equality alone is insufficient.
