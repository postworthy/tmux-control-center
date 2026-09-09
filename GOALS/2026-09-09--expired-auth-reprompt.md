# Goal: Expired authentication recovery

Status: completed
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-09-09
Proposal: `PROPOSALS/2026-09-09--expired-auth-reprompt.md`
Review Boundary: merge from `fix/expired-auth-reprompt` into `main`

## Outcome

Implement user-approved option A in desktop and PWA on the selected server.

## Non-Goals

No cookie lifetime changes, password storage or mutation replay. Deployment and
publication were separately authorized after implementation.

## Acceptance Criteria

- [x] AC1 — Protected-request 401 displays login; inventory cannot dismiss it.
- [x] AC2 — Login clears CSRF and restores manual session creation on the same server.
- [x] AC3 — Regression and canonical verification pass; documentation and review exist.

## Authority Envelope

May continue: local reversible T1 implementation and verification authorized by request.
Must pause: scope expansion, destructive/external/production effects, security
uncertainty, compatibility breaks or unapproved T2/T3 actions.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Authentication recovery | completed | AC1–AC2 | API regression, UI inspection |
| 2. Verification and review | completed | AC3 | ./scripts/verify.sh, review |

## Progress

- 2026-09-09: confirmed eight-hour sliding HTTP cookie and missing mutation-to-login routing.

## Evidence

AC1–AC2: both API regression suites and mounted Chromium desktop/PWA expiry →
wrong key → correct key → manual create checks pass; server and draft retained.
AC3: final canonical verification passes (166 .NET tests, 16 frontend suites,
shell/native/Compose checks), frontend builds pass, diff check passes.
Detailed commands, limits and review: `REVIEWS/2026-09-09--expired-auth-reprompt.md`.

## Discoveries

WebSocket inventory continues independently of expired HTTP authentication.

## Decisions

Use explicit reauthentication, retain client selection state, never replay mutations.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: resolved missing SDK with isolated pinned SDK; browser harness selector corrected; stale PWA socket close callback guarded.

## Next Action

- User can reload the desktop page or apply the PWA update to use the deployed fix.

## Pause Conditions

Authority boundaries above; two unchanged verification failures require reassessment.

## Outcomes

Implementation complete and deployed. User subsequently authorized committing,
merging into main, and pushing to origin/main.

## Deployment follow-up

2026-09-09: user authorized deployment; image `expired-auth-20260909` is live
and healthy on the existing HTTPS origin. Probe, health, authentication boundary
and served bundle checks passed; details and rollback are in the Review Record.
