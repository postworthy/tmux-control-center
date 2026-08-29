# Review Record: Detached Filtering and Guarded Session Termination

Date: 2026-08-14
Review Boundary: merge from `feat/detached-session-highlight` into `main`
Merge Method: `git merge --no-ff feat/detached-session-highlight`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-14--detached-filter-session-kill.md`

## Branch

- Source branch: `feat/detached-session-highlight`
- Target branch: `main`
- Scope is an uncommitted worktree containing the earlier owner-approved FR19
  detached highlight plus the C018 detached filter and guarded kill capability.

## Commits in Scope

- None yet; commit conformance remains a review-boundary follow-up.

## Git Conformance Checklist

- [x] Source branch is a feature branch and no direct commit was made to `main`.
- [ ] Conventional commit(s) and required proposal linkage exist.
- [x] Diff contains no generated web assets, secrets, or unrelated user work.

## Change Summary

- Adds detached highlighting and a non-persistent detached-only filter composed
  with live name search and the recency-derived deck.
- Adds an Admin/CSRF/rate-limited/audited single-session DELETE capability with
  an explicit target-naming UI confirmation and fixed opaque-ID resolution.

## Acceptance Checklist

- [x] UI filtering scope and empty-state behavior match the approved outcome.
- [x] The service accepts only an opaque current-session ID and constructs one
  separated `kill-session -t RAW_ID` argument vector.
- [x] API integration covers success, 404, CSRF, anonymous, rate-limit, bounded
  tmux failure, inventory result, and audit behavior.
- [x] Product, API, architecture, and security documentation are updated.
- [x] Real tmux last-session behavior is reconciled without misreporting an
  already-applied destructive action.
- [ ] Commit conformance and physical mobile UI validation are complete.

## Verification Evidence

Commands run:

```bash
docker run ... tmux-mobile:c017-server-test dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj ...
docker run ... tmux-mobile:c017-server-test dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj ...
PATH=/tmp/tmux-mobile-c018-dotnet:$PATH ./scripts/verify.sh
npm --prefix src/TmuxMobile.Web run build
tmux -L tmux-mobile-c018-review-20260814 kill-session -t '$0'
```

Results:

- Focused infrastructure after correction: 23 passed, four expected opt-in PTY skips.
- Server integration: 44 passed.
- Canonical verification: passed with 27 Core, 23 Infrastructure, 44 Server,
  and four frontend suites; four opt-in PTY tests skipped as documented.
- Production web build passed and generated bundle contained Detached/Kill UI.
- Isolated real tmux: `$0:disposable` was the only session; `kill-session -t
  '$0'` removed it but returned `no server running`, which the current service
  would map to `TmuxCommandException`/503 after the destructive effect.

## Findings

1. **Resolved:** Last-session termination can be applied while tmux returns
   nonzero because its server exits. `KillSessionAsync` now re-resolves the
   opaque target after command/cancellation failure: confirmed absence is
   success, while a still-present target preserves the original failure. Both
   outcomes have focused tests and canonical verification passes.
2. **Low — follow-up:** No commit exists yet, so commit naming/linkage cannot be
   approved.
3. **Low — follow-up:** Physical mobile layout, confirmation, and filter toggle
   remain unobserved; no real owner session may be killed for acceptance.

## Rollback Plan

Revert the eventual C018 commits and restore/redeploy the prior preserved image.
Rollback removes the API/UI but cannot recreate a confirmed killed session.

## Approvals

- Reviewer: AI Agent
- Approval status: ready with explicit follow-ups
- Timestamp: 2026-08-14 16:03 America/Chicago

## Follow-Ups

- Obtain owner approval before deployment/commit/merge/push; physical UI testing
  remains after deployment.
