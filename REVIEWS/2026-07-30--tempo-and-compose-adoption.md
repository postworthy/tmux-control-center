# Review Record: Tempo and Compose Adoption

Date: 2026-07-31
Review Boundary: merge from `chore/c001-adopt-tempo` into `main`
Merge Method: `git merge --no-ff chore/c001-adopt-tempo`
Risk Class: T1
Related Proposal:
`PROPOSALS/2026-07-30--tempo-and-compose-adoption.md`

## Decision

Ready. The local feature branch satisfies its approved scope and may cross the
review boundary if the repository owner separately authorizes the merge.
Production deployment remains outside this decision.

## Branch

- Source branch: `chore/c001-adopt-tempo`
- Target branch: `main`

## Commits in Scope

- `f6d0990` chore(governance): adopt Tempo workflow
- `5f9b717` feat(deploy): add Tailscale-only Compose deployment

The C003 review-record commit is documentation-only evidence created after this
assessment.

## Git Conformance Checklist

- [x] Source branch matches naming policy.
- [x] No direct commit to `main`.
- [x] Commit subjects are conventional.
- [x] Commits contain `Roadmap` and `Proposal` trailers.
- [x] Commits match the approved decomposition.

## Change Summary

- Installed five Tempo portable repo-local skills and its stack-neutral kernel.
- Added approved project contracts, durable decisions, roadmap, proposal,
  living goal, canonical verification, and third-party notice.
- Added a multi-stage ASP.NET/React image and hardened Compose configuration.
- Required an exact Tailscale host IP, HTTPS origin, credentials, non-root
  UID/GID, tmux socket, state directories, and certificate/key mounts.
- Added operational, security, upgrade, rollback, and compatibility guidance.

## Acceptance Checklist

- [x] All five installed skills and routing files appear in the manifest.
- [x] Product contracts match the owner's approved MVP and deployment scope.
- [x] Canonical verification passes.
- [x] Compose renders only an exact host IP and rejects its absence.
- [x] The production image builds and includes tmux 3.3a.
- [x] A disposable non-root/read-only HTTPS container becomes healthy.
- [x] Docs describe rollback and the host/client tmux compatibility risk.
- [x] No secrets, TLS keys, caches, logs, or unrelated user work are tracked.

## Verification Evidence

Commands and results:

```bash
./scripts/verify.sh
```

- Pass after commits `f6d0990` and `5f9b717`.
- 24 Core, 6 Infrastructure, and 9 Server integration tests passed.
- One real-tmux test was intentionally skipped by the canonical gate.
- TypeScript passed.
- Safe Compose example rendered; missing `TAILSCALE_IP` was rejected.

```bash
TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 \
  dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj \
  --filter Category=LinuxIntegration
```

- Pass: one dedicated-socket PTY lifecycle test; no ordinary tmux socket used.

```bash
docker build --tag tmux-mobile:tempo-review .
```

- Pass: image
  `sha256:37b621ee53a4ab1950b40819cf6df74822659ea8ac4040ecd885ac5afe8ea6a2`,
  228,712,036 bytes.
- Image tmux probe: `tmux 3.3a`.

```text
Disposable Compose startup:
127.0.0.1:55443 -> container 5443
GET /health/live -> Healthy
```

- Pass with generated one-day test certificate, isolated empty socket
  directory, UID/GID 1000, read-only root filesystem, and hardened tmpfs.
- Test container and network were stopped and removed.
- Empty isolated socket produced expected inventory warnings; liveness remained
  healthy.

```bash
dotnet list /absolute/path/TmuxMobile.sln package --vulnerable --include-transitive
npm --prefix src/TmuxMobile.Web audit --omit=dev
git diff --check main...HEAD
```

- Pass: no known NuGet/npm vulnerabilities and no whitespace errors.

## Findings

- Blocking: none.
- High: none.
- Medium: none.
- Low: the image uses Debian's tmux 3.3a; a target host running an incompatible
  tmux server requires a matching image package/build before critical use.
- Informational: file-based Tailscale certificates require external renewal;
  the deployment guide makes this an explicit operator responsibility.
- Informational: readiness was not asserted in the empty-socket startup fixture;
  target-host readiness requires a running compatible tmux server.

## Risk, Compatibility, and Rollback

- Risk remains T1 because no production, remote, or host-state action occurred.
- API, browser persistence, and systemd deployment behavior are unchanged.
- Compose cannot render without its security-critical values and has no wildcard
  host-address default.
- Rollback is to revert C003, C002, and C001 in reverse order, then run
  `dotnet test` and the frontend typecheck. The pre-existing MVP remains intact.

## Approvals

- Scope and product contract: repository owner, approved 2026-07-30.
- Reviewer: Codex, evidence-backed local Change Review.
- Review status: ready.
- Timestamp: 2026-07-31 00:09 CDT.
- Merge/push/deployment approval: not requested and not implied.

## Follow-Ups

- On the target host, validate `/health/ready` and terminal attach against a
  disposable session before using critical tmux workloads.
- Establish renewal for the mounted Tailscale certificate before its 90-day
  expiry.
