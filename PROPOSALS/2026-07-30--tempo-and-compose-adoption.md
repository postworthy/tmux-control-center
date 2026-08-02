# Proposal: Tempo and Tailscale-Only Compose Adoption

Date: 2026-07-30
Owner: Human Partner and AI Agent
Risk Class: T1
Related Context: approved Tempo integration and deployment clarification
Roadmap Item: C001-C003
Planned Branch: `chore/c001-adopt-tempo`
Expected Commit Count: 3

## Objective

Adopt Tempo's portable repo-local workflow and make a lightweight, secure Docker
Compose deployment the supported production publishing path.

## Scope

In scope:

- Tempo skills, portable kernel, project contracts, goal, roadmap, verification,
  and review record.
- A multi-stage image and Compose service published only on a required Tailscale
  IP using mounted HTTPS and state material.
- Documentation and verification for the new path.

Out of scope:

- Running a production deployment, generating production secrets/certificates,
  or changing Tailscale policy.
- Public ingress, application feature expansion, or removal of systemd support.

## Expected Files Touched

- `.agents/skills/**`, `.tempo/**`, `AGENTS.md`, `GOALS/**`
- `PROJECT-BRIEF.md`, `SPEC.md`, `DECISIONS.md`, `STATUS.md`
- `ROADMAP/**`, `PROPOSALS/**`, `REVIEWS/**`
- `scripts/verify.sh`, `Dockerfile`, `compose.yaml`, `.dockerignore`
- `deploy/docker/**`, `README.md`, `docs/deployment.md`, `docs/security.md`

## Acceptance Criteria

- [x] All five Tempo portable skills and routing contracts are present.
- [x] Product contracts contain no unresolved placeholders and reflect user
  approval.
- [x] Canonical verification exits zero.
- [x] Compose refuses missing security-critical deployment values and publishes
  only to the configured Tailscale IP.
- [x] The production image builds and contains the ASP.NET application and tmux
  client.
- [x] A review record provides a ready/not-ready decision.

## Verification Plan

```bash
./scripts/verify.sh
docker build --tag tmux-mobile:tempo-review .
```

Pass means canonical verification and the production image build exit zero,
with no unexpected tracked-file changes.

## Change Review Plan

- Review Boundary: merge from `chore/c001-adopt-tempo` into `main`
- Planned Review Record:
  `REVIEWS/2026-07-30--tempo-and-compose-adoption.md`
- Reviewer/approver expectation: evidence-backed agent review; the human retains
  authority for merge, push, certificate, and deployment actions.

## Git Plan

- Existing branch: `chore/c001-adopt-tempo`
- Subjects follow the C001-C003 roadmap.
- Commits include `Roadmap` and `Proposal` trailers.
- Planned merge method: `git merge --no-ff chore/c001-adopt-tempo`

## Decomposition Plan

1. Adopt the Tempo thin slice and project contracts — Verify by inspecting skill
   manifests and contracts — Exit: workflow is resumable — Risk: T1.
2. Add container publishing — Verify by Compose rendering, negative required
   value checks, and image build — Exit: safe deployable artifact exists — Risk:
   T1 — Dependency: unit 1.
3. Review and record evidence — Verify by canonical command, clean diff audit,
   and review checklist — Exit: explicit readiness decision — Risk: T0 —
   Dependency: units 1-2.

Thin slice milestone:

- After unit 1, the next compatible agent can route work through Tempo and run a
  single project-native verification command.

Intentional deferrals:

- Production deployment and physical-device validation.
- Automated certificate renewal and host tmux version pinning.

## Rollback Plan

1. Revert the C003, C002, and C001 commits in reverse order.
2. Existing application and systemd deployment remain unchanged.
3. Run the pre-adoption baseline commands: `dotnet test` and
   `npm --prefix src/TmuxMobile.Web run typecheck`.

## Risks and Mitigations

- Risk: copying starter tooling could replace project conventions.
  Mitigation: use Tempo's portable installer and project-native verification.
- Risk: Docker accidentally publishes on every host interface.
  Mitigation: require an explicit IP in the port mapping and test missing-value
  failure.
- Risk: host and image tmux clients are protocol-incompatible.
  Mitigation: document and require a readiness/attach test before critical use.
- Risk: container secrets or state enter the image.
  Mitigation: mounts, `.dockerignore`, read-only root filesystem, and secret
  pattern review.

## Compatibility / Migration Notes

- API and persisted browser behavior do not change.
- Systemd/nginx remains supported.
- No schema or data migration is needed.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-07-30 America/Chicago
