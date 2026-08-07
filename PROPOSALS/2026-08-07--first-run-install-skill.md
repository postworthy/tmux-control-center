# Proposal: First-Run Install Skill and Tmux Compatibility Gate

Date: 2026-08-07
Owner: Human Partner and AI Agent
Risk Class: T1 for repository work; invoked host/deployment actions retain their
existing T2/T3 approval boundaries
Related Issue/Context: A clean clone on another machine required manual host
setup and tmux client/server compatibility repair.
Roadmap Item: C016
Planned Branch: `feat/c016-first-run-install-skill`
Expected Commit Count: 2

## Objective

Provide a repository-local skill that takes a fresh Linux clone to a safely
configured, compatibility-proven Tailscale Serve deployment while never
silently installing Docker/Tailscale, exposing the login secret, touching the
owner's ordinary tmux sessions, or changing live host state without approval.

## Scope

In scope:

- Diagnose Linux, tmux, Docker Engine plus Compose v2, Tailscale installation,
  daemon/login state, tmux socket ownership, and required local commands.
- Offer explicit-approval tmux installation guidance for detected package
  managers; stop and direct the user to official installation instructions when
  Docker/Compose or Tailscale is missing.
- Generate `deploy/docker/.env`, state directories, and a separate ignored
  `0600` login-key file from validated non-secret host inputs. Generate the key
  internally without printing it; tell the user the path and an explicit command
  they may run themselves to reveal it.
- Parameterize the production image to compile the exact sanitized host tmux
  release from the official upstream release archive.
- Create a uniquely named disposable host tmux server and require the built
  image to query its mounted socket successfully before long-lived startup;
  clean up only that isolated server.
- Guide approved Compose/Tailscale Serve changes, exact-IP listener checks,
  health/readiness/direct-backend checks, and a second-device reachability check.
- Add deterministic script tests, canonical verification, operator docs, and a
  Change Review.

Out of scope:

- Installing or upgrading Docker, Docker Compose, or Tailscale.
- Automatically invoking sudo, changing tailnet ACLs/grants, exposing a public
  listener, or printing/reading the generated secret into agent output.
- Modifying an existing `.env` without an explicit overwrite choice.
- Reading, attaching to, sending input to, renaming, or stopping ordinary tmux
  sessions during compatibility validation.
- Merge, push, publication, or deployment to the current machine without a
  separate explicit request.

## Expected Files Touched

- `.agents/skills/setup-tmux-mobile/**`
- `scripts/first-run-setup.sh`
- `tests/first-run-setup.test.sh`
- `Dockerfile`
- `compose.tailscale-serve.yaml`
- `deploy/docker/.env.example`
- `.gitignore`
- `README.md`
- `deploy/docker/README.md`
- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `STATUS.md`
- `REVIEWS/2026-08-07--first-run-install-skill.md`

## Acceptance Criteria

- [ ] The skill routes fresh-clone/setup requests and clearly separates
  read-only diagnosis from privileged, network, secret, and deployment actions.
- [ ] Preflight identifies missing tmux, Docker/Compose, or Tailscale without
  mutating the host; Docker/Tailscale absence stops with official guidance.
- [ ] Environment generation validates inputs, refuses overwrite by default,
  writes mode-0600 ignored files atomically, never prints the generated key, and
  records host UID/GID, Tailscale IP/hostname, socket, ports, image tag, and exact
  host tmux release.
- [ ] The Docker image builds that exact sanitized tmux release, reports the
  expected `tmux -V`, and Compose fails if `TMUX_VERSION` is missing.
- [ ] The compatibility command proves the container client can query only a
  uniquely named disposable host server and always attempts isolated cleanup;
  failure prevents long-lived startup.
- [ ] The skill verifies Compose rendering, exact Tailscale-IP publication,
  service health, Serve status, HTTPS liveness, direct-backend 426, authenticated
  readiness, and asks for a second-tailnet-device reachability check.
- [ ] Focused tests and `./scripts/verify.sh` pass; docs and review record the
  secret, compatibility, approval, recovery, and rollback boundaries.

## Verification Plan

```bash
bash tests/first-run-setup.test.sh
docker compose -f compose.tailscale-serve.yaml \
  --env-file deploy/docker/.env.example config --quiet
./scripts/verify.sh
```

Pass means script behavior is exercised without host mutation, Compose renders
with the required tmux version, canonical checks exit zero, and review confirms
the documented manual reachability boundary.

## Change Review Plan

- Review Boundary: merge from `feat/c016-first-run-install-skill` into `main`
- Planned Review Record: `REVIEWS/2026-08-07--first-run-install-skill.md`
- Reviewer expectation: verify secret non-disclosure, no unapproved privilege or
  deployment, isolated tmux cleanup, input validation, source-build integrity,
  exact-IP behavior, compatibility gate ordering, tests, docs, and rollback.

## Git Plan

- Branch: `feat/c016-first-run-install-skill`
- Commit: `feat(setup): add first-run deployment skill`
- Trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C016`
  - `Proposal: PROPOSALS/2026-08-07--first-run-install-skill.md`
- Merge method: `git merge --no-ff feat/c016-first-run-install-skill`

## Decomposition Plan

1. Safe configuration thin slice — add preflight and atomic environment/key
   generation plus tests — exit when a fake fresh host produces validated
   ignored files without secret output or overwrite — Risk: T1.
2. Tmux compatibility gate — host-match the image client and add an isolated
   cross-socket probe — exit when exact version and cleanup behavior are proven
   without addressing the default server — Risk: T1.
3. Skill and operator workflow — initialize/validate the repository skill and
   encode approvals, prerequisite guidance, start, Serve, and reachability
   checks — exit when a fresh agent can follow one safe ordered path — Risk: T1.
4. Verification and review — run focused/canonical checks and audit rollback,
   secrets, docs, and history — exit with a ready or blocked Review Record.

Thin slice: after unit 1, a fresh host can safely produce a complete ignored
Compose configuration and private login-key file without deploying anything.

Dependencies and unknowns:

- The host must be Linux and the operator must know/confirm its MagicDNS name.
- The host tmux version must correspond to an official upstream release archive.
- Tailscale CLI syntax may vary; inspect `tailscale serve --help` before changes.

Intentional deferrals:

- Docker/Tailscale installation automation, tailnet policy changes, public
  ingress, non-Linux hosts, and automatic certificate lifecycle management.
- Fully automated remote validation from an independent tailnet device.

## Rollback Plan

1. Revert C016; prior images return to the distribution tmux package behavior.
2. Before an invoked live setup, capture Serve status and any prior image tag;
   restore those independently if the user approved deployment changes.
3. Preserve `.env`, key, data-protection keys, and audits unless the user
   explicitly requests their removal; validate rollback with canonical Compose
   checks and the former health path.

## Risks and Mitigations

- Risk: source-build version injection or untrusted archive selection.
  Mitigation: strict release-token validation, fixed official HTTPS upstream,
  no shell interpolation beyond the validated token, and exact `tmux -V` check.
- Risk: compatibility testing touches user sessions.
  Mitigation: unique high-entropy socket/session names, explicit `-L` on every
  host/container command, trap cleanup, and no default-socket query.
- Risk: secret disclosure through output or source control.
  Mitigation: ignored paths, `umask 077`, atomic files, no secret stdout, no
  shell tracing, no agent readback, and user-only reveal command.
- Risk: an automated setup changes privileged/network state unexpectedly.
  Mitigation: diagnose first and require explicit confirmation before package,
  Serve, Compose-up, firewall, or tailnet changes.

## Compatibility / Migration Notes

- Application API/schema and existing `.env` files remain compatible.
- Existing builds default to the example tmux version; new first-run generation
  pins the detected host release. Rebuilding changes only the image client.
- No persistent-data migration exists.

## Observability / Debug Notes

- Report command status, versions, file paths, container health, exact listener,
  Serve status, HTTP codes, and probe result; never report the key value.
- Protocol/socket failures appear before long-lived startup and leave the
  disposable server cleanup result visible.

## Approval

- Approval status: approved
- Approved at: 2026-08-07 through the owner's explicit request to include a
  fresh-clone setup skill covering tmux, Docker Compose, Tailscale, configuration,
  secret handoff, reachability, and host/container tmux compatibility.
