# Proposal: Session-Scoped App Scroll Preference

Date: 2026-08-19
Owner: Human Partner and AI Agent
Risk Class: T1
Related Issue/Context: App Scroll currently resets on terminal exit and WebSocket
disconnect, forcing repeated enablement for mouse-aware TUIs.
Roadmap Item: C020
Planned Branch: `feat/detached-session-highlight` (current feature worktree)
Expected Commit Count: 1

## Objective

Remember an explicit App Scroll choice on this browser for only the tmux session
where it was enabled, across terminal exit, reconnect, and PWA reload, until the
owner turns it off for that session.

## Scope

In scope:

- Device-local persistence keyed by the opaque session ID.
- New and never-enabled sessions remain default-off.
- Enabling or disabling one session does not change any other session.
- Exit, disconnect, reconnect, terminal remount, and PWA reload preserve the
  selected session's stored state.
- Defensive parsing, bounded retained IDs, unavailable-storage fallback, focused
  tests, product/docs updates, canonical verification, and Change Review.

Out of scope:

- Server-side, cross-browser, cross-device, or account-synchronized preferences.
- Automatic TUI detection, mode changes based on process names, or persistence
  by mutable session name.
- API/WebSocket changes, deployment, commit, merge, push, or publication.

## Expected Files Touched

- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `src/TmuxMobile.Web/src/TerminalView.tsx`
- `src/TmuxMobile.Web/src/applicationScrollPreference.ts`
- `src/TmuxMobile.Web/tests/applicationScrollPreference.test.ts`
- `src/TmuxMobile.Web/tsconfig.tests.json`
- `docs/architecture.md`
- Tempo goal and review records

## Acceptance Criteria

- [ ] An enabled session reopens and reconnects with App Scroll still enabled,
  and reload reads the same device-local preference.
- [ ] A different session remains off unless it was independently enabled.
- [ ] Turning App Scroll off removes that session's enabled preference without
  changing other sessions.
- [ ] Malformed, unavailable, or oversized storage fails safely to default-off
  and retains a bounded unique set of opaque IDs.
- [ ] Existing wheel routing, focus neutrality, coalescing, keyboard behavior,
  terminal connection behavior, and default tmux-history behavior remain intact.

## Verification Plan

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
```

Pass means the preference regression covers isolation and lifecycle semantics,
the frontend builds, canonical verification exits zero, and docs agree.

## Change Review Plan

- Review Boundary: merge from the eventual coherent C020 feature branch into
  `main`; the current dirty C018 worktree is preserved and not committed here.
- Planned Review Record:
  `REVIEWS/2026-08-19--session-scoped-app-scroll-preference.md`
- Reviewer expectation: inspect storage isolation/bounds, lifecycle reset
  removal, default-off behavior, compatibility, rollback, and evidence.

## Git Plan

- Do not switch or rewrite the current dirty C018 worktree.
- Commit subject pattern: `feat(terminal): persist app scroll per session`
- Planned merge method: `git merge --no-ff <coherent-c020-branch>` after the
  owner chooses the integration boundary.

## Decomposition Plan

1. Preference contract thin slice — implement a pure bounded storage helper and
   exact isolation/fallback tests — Verify with frontend unit tests — Risk T1.
2. Terminal lifecycle wiring — initialize from storage, write only on explicit
   toggle, and stop clearing on exit/disconnect — Verify with typecheck/build and
   source lifecycle inspection — Risk T1.
3. Contracts/review — update SPEC/architecture/roadmap, run canonical verification,
   and record Change Review — Risk T1.

Thin slice milestone:

- A pure storage test proves session A can remain enabled across a fresh read
  while session B remains off and disabling A preserves B.

Dependencies and unknowns:

- `localStorage` is already used for session recency and is the established
  device-local persistence boundary.
- Opaque tmux session IDs are authoritative for the current server lifetime;
  stale entries are capped so storage cannot grow without bound.

Intentional deferrals:

- Server-synchronized preferences and explicit settings management UI.

## Rollback Plan

Revert the C020 code/docs. Existing stored preference data becomes inert; no API,
tmux, or persistent server data migration is required. Validate rollback with
frontend tests and `./scripts/verify.sh`.

## Risks and Mitigations

- Risk: one session's mode leaks to another. Mitigation: exact opaque-ID lookup
  and isolation tests.
- Risk: stale or hostile local data breaks terminal entry. Mitigation: strict
  string-array parsing, deduplication, cap, and exception fallback.
- Risk: an old session preference survives forever. Mitigation: bounded
  most-recently-enabled IDs; disabling removes the ID.

## Compatibility / Migration Notes

- API compatibility impact: none.
- Data/schema migration: additive browser-local key only; malformed or absent
  values default off.
- Existing sessions remain off until explicitly enabled after this release.

## Observability / Debug Notes

- No server logs or terminal content are added. The pressed state and routing
  tests are the observable signals.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-19 through the explicit request for App Scroll to persist
  across app connections only for the session where it was enabled.
