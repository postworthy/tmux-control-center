# Review Record: Live Session Search and Guarded Creation

Date: 2026-08-11
Review Boundary: merge from `feat/c017-session-search-create` into `main`
Merge Method: not authorized
Risk Class: T2 scoped process-launch capability
Related Proposal: `PROPOSALS/2026-08-11--session-search-create.md`

## Decision

Ready with explicit physical-device follow-up. The initial blocking name-integrity
finding was corrected and the changed canonical gate passes. The owner-approved
deployment is healthy, and C017 was merged and pushed to GitHub `main`; physical
iPhone acceptance remains outstanding.

## Branch and Scope

- Source branch: `feat/c017-session-search-create`
- Target branch: `main`
- Commits in scope:
  - `416ac6b` `feat(sessions): add search and guarded creation`
  - This review/publication checkpoint commit.
- Scope matches the approved live filter plus fixed-shape create-and-open
  proposal; no unrelated tracked work or generated artifacts appears in the diff.

## Acceptance Mapping

- AC1: pure frontend tests cover partial, case-insensitive, trimmed, cleared,
  no-match, stable-order behavior; all deck consumers use `visibleSessions`.
- AC2: core/infrastructure tests cover create-specific name integrity, fixed
  separated arguments, normalization, opaque ID, invalid input, duplicate
  stderr, and malformed tmux ID.
- AC3: integration tests cover success/audit, invalid input, authentication,
  missing CSRF, duplicate conflict, bounded tmux failure, and inventory refresh;
  source retains the existing `interact` limiter.
- AC4: successful response is promoted and passed directly to terminal mode;
  failure remains in the creation dialog.
- AC5: API/architecture/README/SPEC are aligned and canonical verification
  passes through the documented repo-matched .NET 10 adapter.

## Verification Evidence

```text
docker build --target server-build ...
  PASS; image sha256:3829f7...
docker run --rm tmux-mobile:c017-server-test dotnet test TmuxMobile.sln --no-restore
  PASS; Core 27, Infrastructure 18 with 4 expected isolated skips, Server 40
npm --prefix src/TmuxMobile.Web run typecheck
  PASS
npm --prefix src/TmuxMobile.Web run test:unit
  PASS; 4 suites
PATH=/tmp/tmux-mobile-c017-dotnet:$PATH ./scripts/verify.sh
  PASS; exact canonical script with .NET 10 supplied by the local test image
git diff --check
  PASS
```

The host-native focused .NET command could not start because the host has SDK 8
and `global.json` requires 10.0.300. The container adapter uses the repository's
SDK 10/native-compiler build stage; this is an environment limitation, not a
test failure.

## Findings

- Blocking — name integrity: `InputValidation.ValidateRename` permits `.` and
  `:`, but real tmux creation silently rewrites both to `_`. An isolated command
  created `name.with:chars` and `list-sessions` returned `name_with_chars`.
  `TmuxService.CreateSessionAsync` currently returns the requested normalized
  name, so the response could disagree with authoritative tmux state and distinct
  accepted names could collide after rewriting. Resolved: `ValidateCreateName`
  rejects both characters before process launch; core, infrastructure, and API
  regressions cover them. An isolated probe confirms `a b_@+-` is retained exactly.
- Non-blocking — physical iPhone layout and create/open acceptance require a
  later explicitly approved deployment; no production claim is made here.
- Non-blocking — the global antiforgery exception handler currently maps a
  missing-token exception to 500 in the integration host. C017 safely scopes a
  direct 400 mapping to its new endpoint; unrelated routes are deferred.

## Risk, Compatibility, and Rollback

- Fixed argument separation, opaque IDs, Interact authorization, CSRF, rate
  limiting, auditing, and bounded error mappings are present.
- The route is additive and storage/schema/network/terminal contracts remain
  backward compatible. No migration is required.
- Reverting C017 removes the UI and endpoint without deleting sessions created
  while it existed; those remain ordinary tmux sessions under owner control.
- No deployment, running tmux session, Tailscale configuration, secret, remote,
  or public state was changed. The isolated review session was destroyed.

## Approvals and Next Review

- Owner scope approval: explicit feature request on 2026-08-11.
- Reviewer: Codex agent using the Tempo Change Review workflow.
- Boundary approval: the owner explicitly approved local commit, merge to
  `main`, and push of `main` to the existing GitHub origin on 2026-08-11.
- Changed review completed after create-name integrity evidence and a fresh
  canonical pass.
- Owner separately approved deployment on 2026-08-11. The verified image
  `sha256:80f290...` is healthy on the unchanged app/Serve boundary; C015 image
  `sha256:2bc9c5...` is retained as `tmux-mobile:pre-c017-search-create-rollback`.
  Physical search and create/open acceptance remains an explicit follow-up.
- Publication completed: feature `416ac6b`, review checkpoint `3cf406e`, and
  merge `b7b8e5b`; GitHub `main` advanced from `137b774` to `b7b8e5b`.
