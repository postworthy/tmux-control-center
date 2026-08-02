# Goal: Tailscale-Only Compose Delivery

Status: completed
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-07-30
Proposal: `PROPOSALS/2026-07-30--tempo-and-compose-adoption.md`
Review Boundary: merge from `chore/c001-adopt-tempo` into `main`

## Outcome

The approved tmux mobile MVP can be built and published with Docker Compose over
HTTPS on one explicitly configured Tailscale IP, with Tempo providing durable
execution and review state.

## Non-Goals

- Do not perform the production deployment or modify host/tailnet state.
- Do not broaden application features or remove existing deployment assets.

## Acceptance Criteria

- [x] AC1 — All Tempo skills and project contracts are present and internally
  consistent.
  - Evidence: Tempo manifest plus contracts in commit `f6d0990`; review found no
    unresolved product placeholders.
- [x] AC2 — Canonical verification passes.
  - Evidence: `./scripts/verify.sh` passed after commits `f6d0990` and `5f9b717`;
    39 tests passed, one opt-in test skipped, TypeScript passed, and both Compose
    checks passed.
- [x] AC3 — Compose renders with safe example inputs and fails when the
  Tailscale IP is absent.
  - Evidence: rendered mapping is `100.64.0.10:443 -> 5443`; missing
    `TAILSCALE_IP` reports a required-variable interpolation error.
- [x] AC4 — The production Docker image builds successfully.
  - Evidence: `docker build --tag tmux-mobile:tempo-review .` produced
    `sha256:37b621ee53a4ab1950b40819cf6df74822659ea8ac4040ecd885ac5afe8ea6a2`
    (228,712,036 bytes) with tmux 3.3a.
- [x] AC5 — Deployment and recovery documentation covers the Compose path and
  its tmux compatibility constraint.
  - Evidence: `deploy/docker/README.md`, `docs/deployment.md`, and
    `docs/security.md`.
- [x] AC6 — The Tempo review record approves or clearly rejects the local
  boundary.
  - Evidence:
    `REVIEWS/2026-07-30--tempo-and-compose-adoption.md` records a ready decision.

## Authority Envelope

### May Continue Without Asking

- Approved local, reversible T0/T1 documentation, scripts, Docker assets,
  builds, tests, commits, and review records on the feature branch.

### Must Pause for Approval

- Merge, push, remote publication, actual deployment, certificate or secret
  creation, tailnet changes, destructive actions, production effects,
  compatibility breaks, or unclear security/privacy impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Tempo thin slice | completed | Contracts and canonical command exist | commit `f6d0990` |
| 2. Compose packaging | completed | Safe config renders and image builds | commit `5f9b717`; startup probe |
| 3. Final evidence | completed | Canonical gate passes and review is recorded | canonical gate and review record |

## Progress

- 2026-07-30: owner approved the original MVP as product contract and selected
  the full portable Tempo adoption.
- 2026-07-30: owner constrained publishing to lightweight Docker Compose bound
  only to the Tailscale IP.
- 2026-07-30: official portable installer added five repo-local Tempo skills.
- 2026-07-30: governance and deployment checkpoints committed as `f6d0990` and
  `5f9b717`.
- 2026-07-31: corrected container URL precedence and writable temporary storage
  after the first disposable startup probe; the rebuilt container became
  healthy over HTTPS and was removed after validation.
- 2026-07-31: final canonical verification and Change Review passed.

## Evidence

- Tempo source revision: `ec572a5172442ccdac502982b075c0cb95006ebd`.
- Application baseline: commit `26c8e4d`.
- `./scripts/verify.sh`: pass; 39 passed, one intentionally opt-in test skipped.
- Dedicated-socket Linux PTY test: pass; one passed, zero skipped.
- NuGet and production npm vulnerability audits: zero known vulnerabilities.
- Disposable Compose startup: healthy on `127.0.0.1:55443` with HTTPS,
  non-root UID/GID, read-only root, hardened tmpfs, and isolated fake socket.

## Discoveries

- The existing repository already implements the requested application and
  systemd deployment; container packaging is the remaining delivery delta.
- Docker must carry a tmux client and mount the host owner's tmux socket
  directory, creating a version-compatibility validation requirement.

## Decisions

- Use Tempo's portable profile with project-native .NET/npm verification.
- Use Docker bridge networking with an exact host-address port mapping.
- Terminate HTTPS in Kestrel from mounted PEM certificate/key files.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: the initial disposable container probe exposed URL precedence
  and read-only `/tmp`; one corrected retry passed.

## Next Action

- No implementation action remains; the owner may separately decide whether to
  authorize the merge and later production deployment.

## Pause Conditions

- Pause before any production, remote, secret, certificate, tailnet, merge, or
  destructive action.
- Pause if safe exact-address publication cannot be verified or tmux access
  would require privileged/root execution.

## Outcomes

- Completed locally with all six criteria evidenced and a ready Change Review.
- No merge, push, certificate issuance, tailnet change, or production deployment
  was performed.
