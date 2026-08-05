# Proposal: Session Tiles Ordered by In-App Recency

Date: 2026-08-04
Owner: Human Partner and AI Agent
Risk Class: T1
Related Issue/Context: The owner wants a session opened in terminal mode to
return as the first tile in the main sessions deck.
Roadmap Item: C013
Planned Branch: `feat/c013-session-recency`
Expected Commit Count: 2

## Objective

Order the main session deck by device-local terminal-open recency so returning
from a terminal places that session first, without changing server inventory or
tmux state.

## Scope

In scope:

- Promote a session when its Terminal action is invoked from the deck.
- Persist an ordered list of opaque session IDs in local storage on that device.
- Derive tiles, previous/next navigation, active rail position, and terminal
  lookup from one consistently ordered session list.
- Preserve server order as the stable fallback for sessions without recency.
- Ignore malformed stored data and prune IDs absent from current inventory.
- Add pure frontend tests, documentation, and a Change Review.

Out of scope:

- Using tmux activity timestamps, foreground process output, session creation
  time, or access from clients outside this app.
- Synchronizing order across devices or storing recency on the server.
- Drag-and-drop/manual ordering, favorites, timestamps in the UI, or API changes.
- Merge, push, publication, or changing the paused C012 acceptance state.
- Deployment was initially out of scope; the owner separately approved replacing
  only the existing tailnet test app with the verified C013 image on 2026-08-05.

## Expected Files Touched

- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `src/TmuxMobile.Web/src/App.tsx`
- `src/TmuxMobile.Web/src/sessionRecency.ts`
- `src/TmuxMobile.Web/tests/sessionRecency.test.ts`
- `README.md`
- `docs/architecture.md`
- `STATUS.md`
- `REVIEWS/2026-08-04--session-tile-recency.md`

## Acceptance Criteria

- [ ] Opening Terminal for a session promotes its ID to the front exactly once;
  opening another session places the newer session first.
- [ ] Returning from terminal mode renders the just-opened session as tile 1 and
  starts the newly mounted deck at its top.
- [ ] Refresh, reconnect, inventory replacement, and reload retain valid recency
  while unknown/new sessions keep stable server order after ranked sessions.
- [ ] Malformed local state falls back safely without breaking inventory render.
- [ ] All tile, navigation, active-index, rail, and terminal lookup paths use the
  same derived order; no backend, WebSocket, or tmux contract changes.
- [ ] Focused frontend checks and `./scripts/verify.sh` pass; docs and review
  describe device-local semantics and rollback.

## Verification Plan

Focused commands:

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
```

Canonical command:

```bash
./scripts/verify.sh
```

Pass means pure tests cover promotion, stable ordering, stale IDs, malformed
storage, and immutability; typecheck and the repository gate exit 0.

## Change Review Plan

- Review Boundary: merge from `feat/c013-session-recency` into `main`
- Planned Review Record: `REVIEWS/2026-08-04--session-tile-recency.md`
- Reviewer expectation: verify scope, local persistence, consistent ordering,
  compatibility, rollback, and focused/canonical evidence.

## Git Plan

- Branch: `feat/c013-session-recency`
- Commit: `feat(sessions): order tiles by terminal recency`
- Trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C013`
  - `Proposal: PROPOSALS/2026-08-04--session-tile-recency.md`
- Merge method: `git merge --no-ff feat/c013-session-recency`

## Decomposition Plan

1. Pure ordering contract — implement safe stored-state parsing, MRU promotion,
   pruning, and stable ordering — verify with frontend unit tests — exit when
   deterministic behavior covers malformed/stale/new IDs.
2. Thin-slice deck integration — route Terminal opens and all order consumers
   through the derived list — verify with typecheck and source inspection — exit
   when return renders the promoted tile first without backend changes.
3. Cross-boundary verification — run canonical verification and complete docs
   and review — exit when evidence and rollback are recorded.

Thin slice: after unit 2, opening a terminal and returning visibly moves that
session to the top of the deck on the same device.

Dependencies and unknowns:

- Local storage may contain old, malformed, or unavailable data; parsing and
  writes must fail safely.
- Inventory snapshots replace session objects frequently, so ranking uses opaque
  IDs rather than object identity.

Intentional deferrals:

- Cross-device recency and server-side owner preferences.
- Activity-derived or manually configurable ordering.

## Rollback Plan

Revert the C013 commits. The deck immediately returns to server inventory order;
the unused local-storage entry is inert and may remain without affecting data or
tmux sessions.

## Risks and Mitigations

- Risk: reordering could desynchronize selection, rail, or previous/next controls.
  Mitigation: derive every consumer from one ordered array and test pure order.
- Risk: corrupt/stale local data could hide or duplicate sessions.
  Mitigation: validate string arrays, deduplicate IDs, prune against inventory,
  and append every unranked live session exactly once in stable server order.
- Risk: local persistence reveals opaque identifiers on the device.
  Mitigation: store IDs only, consistent with the existing active-session ID;
  never store names, previews, commands, or terminal content.

## Compatibility / Migration Notes

- API, WebSocket, tmux, schema, authentication, and network behavior: unchanged.
- Existing users begin with server ordering and accumulate recency only by
  opening terminals in this app.

## Observability / Debug Notes

- Inspect the ordered deck and the device-local recency key; no logs, metrics,
  content, or server-side telemetry are added.

## Approval

- Approval status: approved
- Approved at: 2026-08-04 through the owner's explicit feature request that a
  session opened in-app move to the top when returning to the main list.
