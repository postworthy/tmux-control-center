# Proposal: Live Session Search and Guarded Creation

Date: 2026-08-11
Owner: Human Partner and AI Agent
Risk Class: T2
Related Issue/Context: The owner wants live main-screen filtering by session
name and the ability to create a tmux session and enter its terminal in one flow.
Roadmap Item: C017
Planned Branch: `feat/c017-session-search-create`
Expected Commit Count: 2

## Objective

Let the authenticated owner quickly find a session by name or create one from
the main screen and enter it immediately, without introducing arbitrary command
execution or weakening existing tmux target, authentication, or deployment
boundaries.

## Scope

In scope:

- A top-of-main-screen search input that filters after every input event using
  case-insensitive session-name matching.
- Filtering the already recency-ordered client snapshot without mutating server
  inventory, local recency, or tmux state.
- A creation form accepting only a session name.
- A typed API endpoint protected by Interact authorization, CSRF validation,
  the existing interaction rate limiter, and success/failure audit events.
- Infrastructure creation through an argument-vector tmux invocation equivalent
  to `tmux new-session -d -s NAME`, with server-side name validation that
  rejects characters tmux would silently rewrite.
- Returning the new opaque session ID, refreshing inventory, recording in-app
  recency, and opening the created session's terminal immediately.
- Focused frontend, service, and integration tests plus documentation and review.

Out of scope:

- User-supplied commands, arguments, environment, working directory, tmux
  options, socket, remote host, window layout, or startup automation.
- Killing, restarting, cloning, importing, or otherwise managing sessions.
- Fuzzy/content/preview search, server-side search, saved queries, or filtering
  terminal mode.
- Tailnet/system changes. Deployment, merge, push, and publication were initially
  out of scope; on 2026-08-11 the owner separately approved replacing only the
  existing app while preserving its image/state and unchanged Serve mapping,
  then committing C017, merging to `main`, and pushing `main` to the existing origin.

## Acceptance Criteria

- [ ] Search updates on every character, matches names case-insensitively, uses
  the recency-derived order, and clearing it restores all sessions.
- [ ] A no-match state is visible and does not replace the underlying inventory
  empty/error/reconnect states.
- [ ] A valid unique name creates exactly one detached session and returns its
  opaque safe ID; no raw tmux ID or arbitrary command input crosses the API.
- [ ] Successful creation refreshes inventory, promotes the created session in
  device-local recency, and opens its terminal without an extra tile tap.
- [ ] Empty, invalid, duplicate, unauthorized, CSRF-invalid, rate-limited, and
  tmux-failed requests do not open a terminal and return bounded feedback.
- [ ] Audit records distinguish successful and failed create attempts without
  recording terminal content or secrets.
- [ ] Focused tests, typecheck, canonical verification, documentation, and the
  Change Review pass.

## Verification Plan

Focused commands:

```bash
dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
```

Canonical command:

```bash
./scripts/verify.sh
```

## Change Review Plan

- Review Boundary: merge from `feat/c017-session-search-create` into `main`
- Planned Review Record: `REVIEWS/2026-08-11--session-search-create.md`
- Reviewer expectation: inspect capability scope, argument separation, name
  validation, authorization/CSRF/rate-limit/audit coverage, UI state behavior,
  compatibility, rollback, and criterion evidence.

## Decomposition Plan

1. Capability contract — add validated service creation and safe-ID result with
   unit coverage — exit when no command-like user input can enter tmux arguments.
2. API thin slice — expose the protected typed endpoint with audit and negative
   integration coverage — exit when one valid request creates and resolves one
   session and every boundary rejects safely.
3. Main-screen workflow — add live filtering and create/open behavior — exit
   when UI state, recency, refresh, terminal transition, and errors are coherent.
4. Cross-boundary verification — update docs, run canonical verification, and
   complete review — exit when all evidence and rollback are recorded.

Thin slice: a valid create request produces one detached session and returns the
opaque ID through the protected API before frontend integration.

## Rollback Plan

Revert the C017 commits. Existing sessions and recency storage remain valid; the
API route and controls disappear. Sessions created while the feature was in use
remain ordinary owner-controlled tmux sessions and are not destroyed by rollback.

## Risks and Mitigations

- Risk: creation expands the process-launch boundary. Mitigation: accept only a
  validated name and invoke a fixed tmux executable with fixed argument shape;
  expose no command, shell, environment, path, or raw target fields.
- Risk: duplicate/racing requests create surprising sessions. Mitigation: rely
  on tmux's atomic duplicate-name rejection, return conflict, and rate-limit.
- Risk: inventory polling races terminal opening. Mitigation: use the opaque ID
  returned directly by creation, explicitly refresh inventory, and let terminal
  attachment resolve the authoritative target.
- Risk: filtering desynchronizes deck navigation. Mitigation: derive tiles,
  rail, selection, and navigation from one filtered ordered array.

## Compatibility / Migration Notes

The endpoint is additive. Existing inventory, terminal WebSocket, tmux session,
authentication, storage, Compose, and network contracts remain valid.

## Approval

- Approval status: approved
- Approved at: 2026-08-11 through the owner's explicit request to add both live
  session-name filtering and in-app session creation with immediate terminal entry.
