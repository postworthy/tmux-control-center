# Goal: Coalesce Application Scroll Input

Status: paused
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-05
Proposal: `PROPOSALS/2026-08-05--coalesce-application-scroll-input.md`
Review Boundary: merge from `fix/c014-app-scroll-burst` into `main`

## Outcome

A maximum App Scroll gesture preserves its negotiated wheel bytes and scroll
distance while producing one bounded terminal-input message, so the server's
intentional burst limiter no longer disconnects normal application scrolling.

## Non-Goals

- Do not weaken server limits, reduce velocity behavior, hand-encode mouse input,
  change schemas, deploy, merge, push, publish, or absorb unrelated work.

## Acceptance Criteria

- [x] AC1 — Up to 72 ordered xterm wheel reports from one gesture coalesce into
  one exact input value and one bounded serialized WebSocket message.
  - Evidence: focused maximum-gesture unit regression passes.
- [x] AC2 — `TerminalView` buffers only guarded synthetic wheel `onData`, resets
  per gesture, and sends once after dispatch without consuming pending modifiers.
  - Evidence: source inspection and typecheck pass.
- [x] AC3 — Default history, velocity magnitude/direction, Older/Latest, typing,
  paste, reconnect, and server limiter behavior remain unchanged.
  - Evidence: regression/canonical checks and diff review pass.
- [x] AC4 — RCA, docs, rollback, production image, and review evidence agree.
  - Evidence: RCA and pending review record agree; clean production image and
    documentation checks pass.
- [ ] AC5 — Repeated maximum gestures in the deployed PWA produce no reconnect
  and no new `terminal.input.rate-limit` event.
  - Evidence: pending owner-approved deployment and physical regression check.

## Authority Envelope

### May Continue Without Asking

- Approved, local, reversible T0/T1 edits, tests, builds, docs, commits, and
  review artifacts required by C014.

### Must Pause for Approval

- Deployment, merge, push, publication, server-limit changes, schema/backend/tmux
  changes, scope expansion, destructive actions, compatibility breaks, or
  unclear security/privacy impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Coalescing contract | completed | 72 ordered reports produce one bounded serialized message. | Frontend unit tests passed |
| 2. Terminal thin slice | completed | Guarded synthetic output buffers and sends once per gesture. | Typecheck and source inspection passed |
| 3. Cross-boundary evidence | completed | Canonical gate, image, docs, and review pass with limiter unchanged. | `./scripts/verify.sh` and image inspection passed |
| 4. Live regression | pending | Repeated maximum gestures remain connected without limiter warnings. | Physical PWA and live logs pending |

## Progress

- 2026-08-05: owner reported recurrent App Scroll disconnects and requested RCA
  plus a fix.
- 2026-08-05: live logs confirmed three terminal closures caused by
  `Terminal input rate limit exceeded`; RCA traces 72 per-event sends into the
  server's 64-message bucket.
- 2026-08-05: created `fix/c014-app-scroll-burst` from deployed C013 state.
- 2026-08-05: implemented per-gesture xterm output buffering and one bounded
  flush through the existing serializer. Maximum-report regression and
  typecheck pass.
- 2026-08-05: canonical verification and clean production image
  `sha256:6ac99e...` pass. The image retains C013 recency and C012 velocity
  behavior and carries worker release `62ae5cab06ed4b01`; execution pauses at
  the deployment boundary.

## Evidence

- `RCA/2026-08-05--application-scroll-input-burst-disconnect.md` records live,
  implementation, contract, and verification evidence.
- Frontend tests prove 72 distinct worst-coordinate SGR reports retain exact
  byte order and serialize into one message below 12,000 bytes.
- Diff inspection confirms the server's 64-message and 262,144-byte defaults and
  Policy Violation behavior are unchanged.
- Canonical `./scripts/verify.sh` passes with 24 Core, 12 Infrastructure (four
  isolated tests skipped), 33 Server integration, all three frontend suites,
  and Compose validation. The server burst-rejection integration remains green.
- Production image `sha256:6ac99e...` contains only current
  `index-6tEgo_5-.js` and `TerminalView-RY0by5to.js` application bundles plus
  compressed forms. It retains the C013 recency marker and App Scroll and stamps
  worker cache `tmux-mobile-shell-62ae5cab06ed4b01`.

## Discoveries

- Each negotiated wheel report is small; the failure is message-count
  amplification, not the 262,144-byte server bucket or 12,000-byte client bound.
- The existing server limiter behaves exactly as designed and must remain intact.

## Decisions

- Coalesce at the earliest controllable boundary: guarded xterm `onData` output
  for one synthetic gesture.
- Reuse existing terminal serialization and preserve xterm's encoded bytes.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none.

## Next Action

- Commit the verified RCA/C014 correction, then await explicit approval before
  replacing the live C013 test image.

## Pause Conditions

- Pause if coalescing requires private xterm APIs, raw report encoding, server
  limit changes, or affects ordinary input semantics.

## Outcomes

- RCA, local correction, focused/canonical verification, production packaging,
  docs, and review evidence are complete. Deployment, physical regression
  acceptance, merge, and push remain pending.
