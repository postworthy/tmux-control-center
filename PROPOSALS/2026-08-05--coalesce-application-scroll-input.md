# Proposal: Coalesce Application Scroll Input

Date: 2026-08-05
Owner: Human Partner and AI Agent
Risk Class: T1
Related RCA: `RCA/2026-08-05--application-scroll-input-burst-disconnect.md`
Roadmap Item: C014
Planned Branch: `fix/c014-app-scroll-burst`
Expected Commit Count: 2

## Objective

Prevent App Scroll from disconnecting the terminal by coalescing all negotiated
xterm wheel reports from one gesture into one bounded terminal-input send.

## Scope

In scope:

- Buffer `onData` chunks only while one synthetic application-wheel dispatch is
  active.
- Preserve chunk order, concatenate after dispatch, and pass the result once
  through the existing terminal input serializer.
- Preserve velocity thresholds, 72-event cap, xterm negotiation, button bursts,
  modifier isolation, ordinary typing/paste, and default tmux-history routing.
- Add a maximum-gesture serialization regression, focused/canonical checks,
  production image verification, documentation, and review.

Out of scope:

- Raising, disabling, or otherwise weakening server input rate limits.
- Reducing scroll distance, changing velocity bands, hand-encoding mouse reports,
  backend/API/WebSocket schema changes, or app-specific behavior.
- Deployment, merge, push, publication, or unrelated C012/C013 acceptance.

## Expected Files Touched

- `SPEC.md`
- `ROADMAP/COMMIT-PLAN.md`
- `src/TmuxMobile.Web/src/TerminalView.tsx`
- `src/TmuxMobile.Web/src/terminalInput.ts`
- `src/TmuxMobile.Web/tests/terminalInput.test.ts`
- `docs/architecture.md`
- `docs/security.md`
- `STATUS.md`
- `REVIEWS/2026-08-05--coalesce-application-scroll-input.md`

## Acceptance Criteria

- [ ] A maximum 72-report gesture retains all bytes in order and serializes into
  one terminal-input WebSocket message.
- [ ] Repeated application gestures do not share buffered input; empty or failed
  dispatches send no message.
- [ ] Ordinary typing, paste chunking, modifiers, default history, velocity
  magnitude/direction, and Older/Latest behavior remain unchanged.
- [ ] Server rate-limit defaults and behavior remain unchanged.
- [ ] Focused frontend tests, canonical verification, clean production image,
  docs, and Change Review pass.

## Verification Plan

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
./scripts/verify.sh
```

Pass means the maximum-gesture fixture produces one serialized message below the
existing 12,000-byte client envelope, byte order is exact, existing limiter tests
still pass, and no server/config diff weakens protection.

## Change Review Plan

- Review Boundary: merge from `fix/c014-app-scroll-burst` into `main`
- Review Record: `REVIEWS/2026-08-05--coalesce-application-scroll-input.md`
- Reviewer expectation: confirm RCA traceability, one-message amplification
  bound, unchanged server limiter, rollback, and verification evidence.

## Git Plan

- Commit: `fix(terminal): coalesce application scroll input`
- Trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C014`
  - `Proposal: PROPOSALS/2026-08-05--coalesce-application-scroll-input.md`
- Merge method: `git merge --no-ff fix/c014-app-scroll-burst`

## Decomposition Plan

1. Serialization contract — add ordered coalescing and maximum-report regression
   — verify with frontend unit tests — exit at one bounded message.
2. Terminal thin slice — buffer only during synthetic wheel dispatch and send
   once afterward — verify with typecheck/source inspection — exit with all
   other input paths unchanged.
3. Cross-boundary evidence — canonical gate, production image, docs, and review
   — exit with limiter protection unchanged and rollback recorded.

Thin slice: one maximum velocity gesture still generates 72 negotiated xterm
wheel reports but emits one bounded input message rather than 72.

## Rollback Plan

Revert C014 to restore per-report sends. If the live regression reappears, restore
the preserved pre-C014 image; no data or server migration is involved.

## Risks and Mitigations

- Risk: concatenation changes report order or bytes. Mitigation: exact byte-order
  regression over 72 distinct report tokens.
- Risk: gesture buffers leak across input types or gestures. Mitigation: reset at
  dispatch start and completion; only the synthetic guard writes the buffer.
- Risk: a combined value exceeds input bounds. Mitigation: keep the 72-event cap
  and pass through the existing 12,000-byte chunking serializer.

## Compatibility / Migration Notes

- Server limiter, WebSocket schema, API, tmux, data, authentication, and network
  behavior remain unchanged. No migration is required.

## Observability / Debug Notes

- Existing `terminal.input.rate-limit` logs/audits remain the runtime guard.
- Post-deployment verification must produce repeated maximum gestures without a
  new rate-limit event or disconnect.

## Approval

- Approval status: approved
- Approved at: 2026-08-05 through the owner's explicit request for RCA and fix
  after reporting App Scroll disconnects.
