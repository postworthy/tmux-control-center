# Review Record: First-Run Install Skill and Tmux Compatibility Gate

Date: 2026-08-07
Review Boundary: merge from `feat/c016-first-run-install-skill` into `main`
Merge Method: `git merge --no-ff feat/c016-first-run-install-skill`
Risk Class: T1 repository change; invoked privileged/network/deployment actions
retain explicit T2/T3 approval boundaries
Related Proposal: `PROPOSALS/2026-08-07--first-run-install-skill.md`
Related RCA: `RCA/2026-08-07--first-run-compatibility-probe-failure.md`

## Decision

Ready with explicit fresh-host and live-deployment follow-ups. The owner
explicitly authorized merge and push to GitHub `main` on 2026-08-07. This does
not authorize Tailscale changes, secret display, or replacement of the running
deployment.

## Branch

- Source branch: `feat/c016-first-run-install-skill`
- Target branch: `main`

## Commits in Scope

- `54c8b0a` `feat(setup): add first-run deployment skill`
- This review-record checkpoint commit.

## Git Conformance Checklist

- [x] Source branch matches naming policy.
- [x] No direct commit to `main`.
- [x] Commit subject is conventional.
- [x] Commit includes C016 `Roadmap` and `Proposal` trailers.
- [x] Commit matches the approved proposal and four-unit decomposition.
- [x] No unrelated changes, generated builds, local environment, key, state,
  probe sockets, logs, or containers enter the diff.
- [x] Gitleaks v8.30.1 reports no findings across all 29 commits.

## Change Summary

- Add a discoverable `$setup-tmux-mobile` repository skill with explicit pauses
  around package, privilege, secret-display, Serve, and long-lived deployment
  actions.
- Add a deterministic helper that diagnoses prerequisites, validates non-secret
  inputs, generates atomic ignored mode-`0600` env/key files without key output,
  and refuses replacement by default.
- Build the exact sanitized host tmux release from its official upstream archive
  and assert its version in both build and final runtime stages.
- Gate startup on a uniquely named host/container socket probe with success,
  early-failure, process, and stale-inode cleanup.
- Add focused negative tests, canonical Compose missing-version enforcement,
  operator/security docs, goal evidence, and RCA controls.

## Acceptance Checklist

- [x] Scope matches the approved proposal and decomposition.
- [x] Preflight covers Linux, tmux, Docker daemon/Compose, and Tailscale states;
  Docker/Tailscale installation remains a documented stop.
- [x] Configuration/key generation validates host/IP/ports/tokens, uses 32 bytes
  of randomness, writes mode `0600`, prints no key, and refuses overwrite.
- [x] Compose requires `TMUX_VERSION`; image `sha256:01af4b...` builds tmux 3.4,
  final-stage execution passes, and `ldd` has no unresolved dependency.
- [x] Success and Docker-failure probe paths clean the unique server; the real
  one-off container queried the host tmux 3.4 server and left no probe socket.
- [x] Skill validation and inspection cover exact listener, health, Serve,
  direct-backend 426, authenticated readiness, user-only key reveal, and an
  independent tailnet-device check without claiming it has already happened.
- [x] Docs, rollback, RCA, and canonical verification agree with behavior.

## Verification Evidence

Commands run:

```bash
bash tests/first-run-setup.test.sh
python3 /home/landon/.codex/skills/.system/skill-creator/scripts/quick_validate.py \
  .agents/skills/setup-tmux-mobile
docker compose -f compose.tailscale-serve.yaml \
  --env-file /tmp/tmux-mobile-c016-probe.afbgk3.env build app
docker run --rm --entrypoint /usr/bin/ldd \
  tmux-mobile:c016-probe /usr/bin/tmux
./scripts/first-run-setup.sh probe-tmux \
  --env-file /tmp/tmux-mobile-c016-probe.afbgk3.env
PATH=/tmp/tmux-dotnet10:$PATH ./scripts/verify.sh
docker run --rm -v /home/landon/code/tmux-control-center:/repo:ro \
  ghcr.io/gitleaks/gitleaks:v8.30.1 git /repo --no-banner --redact
```

Results:

- Focused setup suite passes prerequisite, invalid-input, permission,
  non-disclosure, overwrite, compatibility-success, and failure-cleanup paths.
- Skill validation passes.
- Host-matched image builds and final runtime reports tmux 3.4; all dynamic
  libraries resolve.
- Real isolated socket query passes; no `tmux-mobile-probe-*` socket remains and
  the long-lived app/Serve mapping is unchanged.
- Canonical verification passes: 24 Core, 12 Infrastructure, and 33 Server
  integration tests; four opt-in isolated tests skipped; frontend typecheck and
  three unit suites; setup suite; missing-variable and normal Compose checks.
- Full-history Gitleaks scans 29 commits and reports no leaks.

## Risk, Compatibility, and Rollback

- Strict token validation bounds source URL construction. Unavailable or
  unsupported upstream releases fail the image build before deployment.
- API, schema, state, tmux sessions, Tailscale policy, and current live runtime
  are unchanged. Existing manual env files need `TMUX_VERSION` before their next
  Compose render/build; the helper generates it for fresh installs.
- Revert C016 to restore the distribution tmux image behavior. If the skill is
  later invoked for a live host, restore captured Serve status and the prior
  image tag while preserving env, data-protection keys, and audits.

## Findings

- Blocking: none.
- Non-blocking: run the skill from an independent fresh clone/host to validate
  its package-manager guidance and that host's upstream tmux release availability.
- Non-blocking: live Compose/Serve start, browser login, authenticated readiness,
  and second-tailnet-device reachability remain deliberately approval-gated.

## Approvals

- Reviewer: Codex agent under the Tempo Change Review process.
- Approval status: ready with explicit follow-ups.
- Owner scope approval: 2026-08-07 feature request.
- Merge/push approval: explicitly granted by the owner on 2026-08-07.
- Timestamp: 2026-08-07 17:48 CDT.

## Follow-Ups

- On the target fresh host, invoke `$setup-tmux-mobile` and retain its actual
  preflight, version, compatibility, and reachability evidence.
- Cross the merge/push boundary only after separate owner authorization.
