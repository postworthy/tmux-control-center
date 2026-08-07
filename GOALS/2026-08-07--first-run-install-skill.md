# Goal: First-Run Install Skill and Tmux Compatibility Gate

Status: completed
Owner: Human Partner and AI Agent
Risk: T1 repository work; invoked host/deployment actions retain T2/T3 pauses
Updated: 2026-08-07
Proposal: `PROPOSALS/2026-08-07--first-run-install-skill.md`
Review Boundary: merge from `feat/c016-first-run-install-skill` into `main`

## Outcome

A fresh Linux clone contains a discoverable skill that safely prepares a
host-specific deployment, proves its image tmux client against an isolated host
server, and guides an approved Tailscale-only start through reachability checks.

## Non-Goals

- Do not install Docker/Tailscale, alter tailnet policy, expose secrets, touch
  ordinary tmux sessions, deploy this machine, merge, push, or publish.

## Acceptance Criteria

- [x] AC1 — Skill and preflight cover tmux, Docker Compose, and Tailscale with
  clear non-mutating diagnostics and approval boundaries.
  - Evidence: skill validation and focused success/missing/down fake-command tests pass.
- [x] AC2 — Atomic ignored configuration generation produces safe host values
  and a private unprinted login key while refusing overwrite by default.
  - Evidence: focused filesystem, mode, output, invalid-input, and overwrite tests pass.
- [x] AC3 — The image builds the exact validated host tmux release and Compose
  requires the version input.
  - Evidence: missing-version Compose regression, final-stage assertion, image
    `ldd`, and tmux 3.4 image build pass.
- [x] AC4 — An isolated named-socket probe proves client/server compatibility,
  attempts cleanup on every exit, and gates long-lived startup.
  - Evidence: success/failure cleanup tests and real isolated container/socket
    probe pass; no probe socket remains.
- [x] AC5 — The skill covers approved start/Serve actions and verifies listener,
  health, denial, readiness, and second-device reachability without claiming
  remote success from local evidence alone.
  - Evidence: validated skill workflow inspection and canonical verification pass.

## Authority Envelope

### May Continue Without Asking

- Approved local reversible T0/T1 source, tests, docs, Docker image builds,
  isolated uniquely named tmux probes, skill initialization/validation, commits,
  and review artifacts. Do not read generated secrets.

### Must Pause for Approval

- Sudo/package installation, Docker/Tailscale installation, Tailscale login or
  Serve changes, firewall/tailnet policy, long-lived Compose start/replacement,
  secret display, destructive cleanup outside the unique probe, deployment,
  merge, push, publication, compatibility breaks, or unclear security effects.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Configuration thin slice | completed | Preflight and safe env/key generation pass focused tests. | Focused suite passes |
| 2. Compatibility gate | completed | Host-matched image and isolated probe pass without default-session access. | Image/ldd/real probe pass |
| 3. Skill workflow | completed | Initialized skill validates and covers the complete guarded flow. | Skill validation and inspection pass |
| 4. Verification/review | completed | Canonical verification and review evidence pass. | Canonical and Change Review pass |

## Progress

- 2026-08-07: owner approved the fresh-clone setup outcome and required tmux,
  Compose, Tailscale, safe secret/config handoff, reachability, and explicit
  host/container tmux compatibility.
- 2026-08-07: created `feat/c016-first-run-install-skill` from synchronized
  `main` and recorded C016 scope and execution boundaries.
- 2026-08-07: skill validation, fake-host configuration/probe tests, Compose
  render, and host-matched image build passed. The first real probe failed before
  socket query because the runtime omitted split `libevent_core`; its EXIT trap
  also lost function-local cleanup state. RCA recorded before correction.

## Evidence

- `bash tests/first-run-setup.test.sh`: pass for the initial success path.
- Skill `quick_validate.py`: pass.
- C016 image build: pass and build-stage client reports tmux 3.4.
- First real isolated probe: expected failure gate activated; runtime loader and
  cleanup defects recorded in
  `RCA/2026-08-07--first-run-compatibility-probe-failure.md`.
- 2026-08-07: corrected the runtime dependency and scope-stable cleanup. Final
  image `sha256:01af4b...` reports tmux 3.4 with no unresolved libraries and
  successfully queried the isolated host server. Post-pass inspection found a
  non-listening stale probe inode, so exact unique-socket removal was added
  before the final retry.
- 2026-08-07: final focused suite covers prerequisite failures, invalid input,
  secret non-output, modes, overwrite refusal, compatibility success, and Docker
  failure cleanup. Final real probe passes and leaves no probe socket; canonical
  verification passes.
- 2026-08-07: Change Review found no blockers and classified C016 ready with
  fresh-host and live-deployment follow-ups. Full-history Gitleaks found no
  leaks; merge/push remain outside current authority.

## Discoveries

- Existing docs only advise a post-build compatibility check; they do not gate
  long-lived startup or match the image client to the host release.
- The login credential must remain recoverable by the user, so it is a random
  secret rather than a one-way hash; generation can still avoid agent exposure.

## Decisions

- Keep Docker Engine/Compose and Tailscale as prerequisites rather than silently
  installing privileged network/container software.
- Compile the validated host tmux release in the image and still prove protocol
  compatibility over a uniquely named mounted socket before startup.
- Store the key in both the ignored Compose env and a separate ignored `0600`
  handoff file; report only its path and user reveal command.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: resolved by exact runtime dependency, final-stage assertion,
  scope-stable EXIT state, failure-path test, and exact stale-socket cleanup.

## Next Action

- Await owner authorization for merge/push or invoke `$setup-tmux-mobile` from
  the independent fresh host for the recorded follow-up.

## Pause Conditions

- Pause if implementation needs unapproved privileged/network/live changes,
  cannot avoid secret exposure, cannot isolate the compatibility probe, or
  cannot reproduce the host tmux release from a validated upstream token.

## Outcomes

- Implementation, skill validation, focused/canonical checks, host-matched image,
  RCA correction, isolated real compatibility evidence, secret scan, and Change
  Review are complete. C016 is ready with approval-gated fresh-host/live follow-ups.
