# Review Record: Session-Scoped App Scroll Preference

Date: 2026-08-19
Review Boundary: deployed C020 implementation before commit integration
Merge Method: eventual `git merge --no-ff <coherent-c020-branch>`
Risk Class: T1
Related Proposal(s):
`PROPOSALS/2026-08-19--session-scoped-app-scroll-preference.md`

## Branch

- Source branch: current dirty `feat/detached-session-highlight` worktree; C020
  is reviewed as an exact file subset and must be integrated coherently later.
- Target branch: `main`

## Commits in Scope

- None. The owner has not authorized commit/merge, and the worktree contains
  preserved C018 and C019 work outside this review.

## Git Conformance Checklist

- [x] No direct commit to `main`
- [ ] Dedicated source branch matches C020 naming policy
- [ ] Commit subjects are conventional
- [ ] Commit trailers include `Roadmap` and `Proposal`
- [x] Reviewed file subset matches the approved proposal/decomposition

## Change Summary

- Added a guarded local preference storing at most 128 unique enabled opaque
  session IDs, with strict parsing and unavailable-storage fallback.
- Terminal entry reads the selected session's preference; only an explicit
  toggle writes it. Mount, terminal exit, WebSocket close, and reconnect no
  longer clear App Scroll.
- Updated product, roadmap, README, and architecture contracts to supersede the
  old non-persistent lifecycle while preserving new-session default-off behavior.

## Acceptance Checklist

- [x] Scope matches approved proposal/decomposition
- [x] AC1: fresh reads persist one session and isolate every other session
- [x] AC2: disable, malformed/unavailable data, deduplication, and cap pass
- [x] AC3: lifecycle wiring reads on entry and has no exit/disconnect reset
- [x] AC4: routing/coalescing/focus/default behavior and canonical gate pass
- [x] AC5: product and architecture documentation plus rollback agree
- [x] Rollback path is clear and requires no server/data migration

## Verification Evidence

Commands run:

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
PATH=/tmp/tmux-dotnet10:$PATH DOTNET_ROOT=/tmp/tmux-dotnet10 ./scripts/verify.sh
git diff --check
```

Results:

- Frontend unit: five suites passed, including the new preference suite.
- Typecheck and production Vite build passed; service worker stamped release
  `c02de5582fead907`, and the terminal chunk contains the new storage key.
- First canonical attempt reached the expected toolchain boundary: host SDK
  8.0.130 cannot satisfy pinned 10.0.300.
- Canonical rerun through the established repository-matched .NET 10.0.302 SDK
  passed: 27 Core, 23 Infrastructure with four expected opt-in skips, 44 server
  integration tests, and all five frontend suites.
- `git diff --check` passed.
- Deployment built image
  `sha256:60d2e566dbf0cd2ccd22d75e775e0dba1341a1992ef53b2d70b722e19255c602`
  and passed the isolated host/container tmux 3.4 socket probe.
- The replacement app container is healthy with zero restarts. Serve still maps
  `:8443` to the exact `100.85.13.102:8780` bind; HTTPS root/liveness return
  200, anonymous API access returns 401, direct HTTP returns 426, and the live
  terminal bundle contains the new preference key.

## Findings

- No implementation-blocking finding.
- Follow-up: physical PWA acceptance must prove enable A, exit/reopen A,
  disconnect/reconnect A, and open never-enabled B before production acceptance.
- Follow-up: the mixed dirty worktree is not merge-ready as a branch; isolate or
  integrate C020 coherently before commit review.
- Informational: opaque session identity follows the application's existing
  device-local recency convention and is bounded to prevent unbounded stale data.

## Rollback Plan

1. Revert only the C020 preference helper, TerminalView wiring, test, generated
   web assets, and contract changes.
2. The old behavior resumes; the unused browser-local key is harmless and can
   remain without affecting API, tmux, authentication, or terminal content.
3. Rerun frontend tests/build and canonical verification.

## Approvals

- Reviewer: AI Agent
- Approval status: deployed and ready with explicit follow-ups for physical
  acceptance and coherent git integration
- Timestamp: 2026-08-19 America/Chicago

## Follow-Ups

- Perform the physical session A/B persistence matrix after deployment.
- Create/commit on a coherent branch only after preserving the existing C018 and
  C019 work; merge/push remain separate approval boundaries.
