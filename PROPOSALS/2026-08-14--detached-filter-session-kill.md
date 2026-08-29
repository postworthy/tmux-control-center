# Proposal: Detached Filtering and Guarded Session Termination

Date: 2026-08-14
Owner: Human Partner and AI Agent
Risk Class: T2
Related Issue/Context: The owner wants to isolate sessions with no attached
terminal and terminate an unwanted tmux session from its quick-actions menu.
Roadmap Item: C018
Planned Branch: `feat/detached-session-highlight`
Expected Commit Count: 2

## Objective

Let the authenticated owner filter the main deck to detached sessions and
deliberately terminate one exact tmux session without exposing arbitrary tmux
commands or making detached sessions implicitly disposable.

## Scope

In scope:

- An explicit All/Detached filter composed with the existing live name search
  and recency ordering.
- Coherent filtered cards, rail, selection, navigation, and empty-state copy.
- A quick-menu action that opens an in-app confirmation naming the target.
- A typed single-session DELETE endpoint protected by Admin authorization,
  CSRF validation, the existing interaction rate limiter, inventory-safe target
  resolution, bounded errors, inventory refresh, and success/failure auditing.
- A fixed argument-vector tmux operation equivalent to `kill-session -t RAW_ID`
  after opaque-ID resolution; no shell invocation or client-supplied raw target.
- Focused frontend, infrastructure, and integration tests, product contracts,
  canonical verification, and Change Review.

Out of scope:

- Bulk kill, automatic cleanup, kill-by-name, arbitrary tmux commands, killing
  panes/windows/processes directly, remote hosts, schedules, or stale-session
  inference.
- Treating detached sessions as abandoned or safe to kill without confirmation.
- Deployment, commit, merge, push, or publication without separate approval.

## Expected Files Touched

- `SPEC.md`
- `src/TmuxMobile.Core/Abstractions.cs`
- `src/TmuxMobile.Infrastructure/TmuxService.cs`
- `src/TmuxMobile.Server/Program.cs`
- `src/TmuxMobile.Web/src/App.tsx`
- `src/TmuxMobile.Web/src/SessionCard.tsx`
- `src/TmuxMobile.Web/src/api.ts`
- `src/TmuxMobile.Web/src/sessionFilter.ts`
- `src/TmuxMobile.Web/src/styles.css`
- focused test projects and Tempo evidence

## Acceptance Criteria

- [ ] All/Detached filtering updates immediately, composes with name search,
  preserves recency order, and drives cards, rail, selection, and navigation
  from one visible array.
- [ ] The UI distinguishes no sessions from no name matches and no detached
  matches, and clearing filters restores the full ordered deck.
- [ ] Kill is available only through a named confirmation and cancellation sends
  no request; confirmation disables repeated submission and reports failures.
- [ ] One valid Admin-protected DELETE resolves one opaque ID, invokes one fixed-shape
  `kill-session` argument vector, refreshes inventory, audits success, and
  removes the target from the deck.
- [ ] Unknown targets, invalid/absent CSRF, anonymous access, rate limits, and
  tmux failure cannot terminate another session and return bounded responses.
- [ ] Existing session creation, terminal, scrolling, search, authentication,
  deployment, and tmux compatibility remain intact.

## Verification Plan

```bash
dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
./scripts/verify.sh
```

Pass means focused negative and positive paths pass, canonical verification exits
zero, and evidence maps to every acceptance criterion.

## Change Review Plan

- Review Boundary: merge from `feat/detached-session-highlight` into `main`
- Planned Review Record: `REVIEWS/2026-08-14--detached-filter-session-kill.md`
- Reviewer expectation: inspect destructive capability scope, opaque target
  resolution, argument separation, confirmation behavior, auth/CSRF/rate-limit/
  audit coverage, compatibility, rollback, and criterion evidence.

## Git Plan

- Existing branch: `feat/detached-session-highlight`
- Commit subject pattern: `feat(sessions): add detached management controls`
- Planned merge method: `git merge --no-ff feat/detached-session-highlight`

## Decomposition Plan

1. Capability contract — add opaque-ID service termination with exact argument
   coverage — exit when no raw/client command can cross the boundary — Risk T2.
2. Protected API thin slice — add DELETE, audit, refresh, and bounded negative
   paths — exit when one valid request kills exactly one resolved target — Risk T2.
3. Main-screen workflow — add composable detached filter and guarded quick-menu
   confirmation — exit when all deck consumers and error states agree — Risk T1.
4. Verification/docs/review — run all gates and record rollback/evidence — exit
   when the change is review-ready — Risk T1.

Thin slice: a protected DELETE request terminates exactly one inventory-resolved
session and refreshes the authoritative inventory before UI integration.

Intentional deferrals: bulk actions, automatic orphan cleanup, age policies,
undo/recovery, and local-versus-remote client classification.

## Rollback Plan

Revert the C018 commits and redeploy the prior preserved image. This removes the
filter and DELETE capability but cannot restore a session the owner already
confirmed and terminated; that irreversibility is stated in the confirmation.

## Risks and Mitigations

- Risk: terminating the wrong or valuable session. Mitigation: opaque inventory
  resolution, target name in a modal confirmation, one target per request, no
  implicit detached cleanup, and no optimistic disappearance.
- Risk: forged or repeated destructive requests. Mitigation: existing auth,
  CSRF, interaction rate limiter, disabled in-flight UI, audit, and server-side
  target resolution immediately before tmux invocation.
- Risk: attached clients are unexpectedly disconnected. Mitigation: confirmation
  states that the session and its programs will end; the action is not limited
  to detached sessions because it belongs to each session's own menu.

## Compatibility / Migration Notes

The DELETE route and UI controls are additive. There is no schema migration.
Older clients remain compatible; rolling back cannot recreate terminated state.

## Observability / Debug Notes

Audit action `session.kill` records safe target ID, subject, and success only.
Inventory refresh and bounded app logs expose operational failures without
terminal content.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-14 through the owner's explicit request for detached
  filtering and ellipsis-menu tmux session termination.
