# Goal: Focus-Neutral Application Scroll

Status: active
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-05
Proposal: `PROPOSALS/2026-08-05--focus-neutral-application-scroll.md`
Review Boundary: merge from `fix/c015-app-scroll-keyboard-focus` into `main`

## Outcome

App Scroll swipes, its toggle, and application-mode Older/Latest scroll without
focusing xterm's textarea, opening the iPhone keyboard, or disconnecting.

## Non-Goals

- Do not change intentional typing focus, wheel semantics, rate limits, backend
  contracts, merge, push, publication, or unrelated behavior.

## Acceptance Criteria

- [x] AC1 — App Scroll dispatch and toggle make no xterm/textarea focus call.
  - Evidence: complete focus-caller inspection passes.
- [x] AC2 — Reconnect, typing keys, modifiers, and paste retain intentional focus.
  - Evidence: source inspection and typecheck pass.
- [x] AC3 — Velocity, coalescing, Older/Latest, default history, recency, and
  server limiter behavior remain compatible.
  - Evidence: focused/canonical/image checks pass.
- [ ] AC4 — Live iPhone swipes in both directions and application-mode
  Older/Latest keep the keyboard closed and terminal connected.
  - Evidence: pending owner-approved deployment and physical acceptance.

## Authority Envelope

### May Continue Without Asking

- Approved local T0/T1 edits, tests, builds, docs, commits, review artifacts, and
  redeployment of the verified C015 image to the existing tailnet test app while
  preserving rollback and established boundaries.

### Must Pause for Approval

- Merge, push, publication, broader keyboard/focus changes, server/backend/tmux
  changes, scope expansion, destructive actions, compatibility breaks, or
  unclear security/privacy effects.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Focus correction | completed | Wheel dispatcher and toggle are focus-neutral; typing focus remains. | Source inspection and typecheck passed |
| 2. Compatibility | completed | C012–C014 and canonical checks pass unchanged. | Frontend and canonical checks passed |
| 3. Deploy/accept | in_progress | Verified image is healthy; physical wheel actions keep keyboard closed. | Image passed; deployment pending |

## Progress

- 2026-08-05: owner reported keyboard opening on every App Scroll direction and
  application-mode Older/Latest and approved RCA, fix, and redeployment.
- 2026-08-05: RCA confirmed the shared dispatcher and toggle explicitly focus
  xterm; xterm maps focus directly to its hidden textarea.
- 2026-08-05: created `fix/c015-app-scroll-keyboard-focus` from deployed C014.
- 2026-08-05: removed only the shared application-wheel dispatcher and App
  Scroll toggle focus calls. Complete caller inspection, all frontend tests, and
  typecheck pass.
- 2026-08-05: canonical verification and clean production image
  `sha256:2bc9c5...` pass. The image retains C013 recency and C014 App Scroll
  coalescing and carries worker release `f3ef9d969ed07f10`.
- 2026-08-05: committed C015 as `47cc75e`, preserved live C014 image
  `sha256:6ac99e...` as `tmux-mobile:pre-c015-focus-rollback`, tagged C015 image
  `sha256:2bc9c5...` as `tmux-mobile:c015-focus-neutral`, and recreated only the
  existing app. Post-deployment checks pass; physical keyboard acceptance is
  pending.

## Evidence

- `RCA/2026-08-05--application-scroll-opens-keyboard.md` records exact caller and
  installed-xterm evidence.
- Remaining focus callers are limited to connection/reconnection, actual
  terminal keys, Ctrl/Alt modifiers, and paste completion.
- Canonical verification passes with 24 Core, 12 Infrastructure (four isolated
  tests skipped), 33 Server integration, all three frontend suites, and Compose
  validation; server burst rejection remains green.
- Production image `sha256:2bc9c5...` contains only current
  `index-lZ3iXJV5.js` and `TerminalView-DZDci13l.js` application bundles plus
  compressed forms, with recency/App Scroll markers and worker cache
  `tmux-mobile-shell-f3ef9d969ed07f10`.
- Live C015 deployment is healthy on unchanged exact bind
  `100.85.13.102:8780`; HTTPS liveness/root return 200, direct backend root
  returns 426, readiness returns Healthy, and all 13 tmux sessions are visible.
  Root/terminal/worker identity matches C015, C013 recency and App Scroll markers
  remain, and startup logs contain no new errors or rate-limit warning.

## Discoveries

- Wheel input is negotiated through DOM wheel events and does not require
  terminal textarea focus.
- Focus is still intentional for connect, keys, modifiers, and paste.

## Decisions

- Remove only `dispatchApplicationWheel` and `toggleApplicationScroll` focus.
- Require physical iPhone acceptance because desktop automation has no software
  keyboard boundary.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none.

## Next Action

- Owner applies/reloads the PWA update, dismisses the keyboard, and verifies both
  App Scroll directions plus application-mode Older/Latest keep it closed and
  keep the terminal connected; then inspect logs for rate-limit absence.

## Pause Conditions

- Pause if wheel delivery requires focus, typing focus regresses, or correction
  requires global keyboard suppression or xterm internals.

## Outcomes

- RCA, local correction, canonical verification, production packaging, and live
  deployment checks are complete. C015 is healthy with C014 preserved for
  rollback; physical acceptance, final review, merge, and push remain pending.
