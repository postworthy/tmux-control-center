# Goal: Detached Filtering and Guarded Session Termination

Status: paused
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-14
Proposal: `PROPOSALS/2026-08-14--detached-filter-session-kill.md`
Review Boundary: merge from `feat/detached-session-highlight` into `main`

## Outcome

The owner can isolate detached sessions in the main deck and deliberately
terminate one exact tmux session from its quick-actions menu through a protected,
confirmed, observable operation.

## Non-Goals

- No automatic/bulk cleanup, kill-by-name, arbitrary tmux commands, remote-host
  control, undo promise, deployment, merge, push, or publication.

## Acceptance Criteria

- [x] AC1 — All/Detached filtering composes with live name search and preserves
  coherent recency, deck, rail, selection, navigation, and empty states.
  - Evidence: filter unit tests cover attachment/name composition, restoration,
    non-mutation, and order; one `visibleSessions` array drives all consumers.
- [x] AC2 — Kill requires a target-naming in-app confirmation; cancel sends
  nothing, in-flight submission cannot repeat, and errors remain actionable.
  - Evidence: source inspection and production/typecheck builds show the menu
    opens a named modal, Cancel has safe initial focus, and Kill disables while pending.
- [x] AC3 — An Admin-protected DELETE resolves an opaque ID and invokes one fixed-shape
  kill against exactly that current session, then refreshes inventory and audits.
  - Evidence: 23 infrastructure and 44 server integration tests pass; exact
    resolution/kill arguments, inventory removal, and success audit are asserted.
- [x] AC4 — Unknown, anonymous, CSRF-invalid, rate-limited, and tmux-failed paths
  are bounded and cannot terminate a different target.
  - Evidence: server integration tests cover 404, 401, 400 CSRF, 429, and 503;
    reconciliation tests distinguish applied last-session kills from real failures.
- [x] AC5 — Existing workflows remain compatible; product docs, canonical
  verification, rollback, and Change Review are complete.
  - Evidence: canonical verification passes; production web build passes; docs
    agree; Change Review is ready with deployment/commit/physical follow-ups.

## Authority Envelope

### May Continue Without Asking

- The owner's 2026-08-14 request explicitly authorizes local implementation of
  the scoped T2 single-session termination capability and detached filter,
  including fixed-shape tmux invocation, API/UI integration, tests, docs, and
  non-destructive test doubles or isolated tmux sessions.
- Local reversible T0/T1 corrections within this recorded scope.

### Must Pause for Approval

- Killing any real user tmux session during verification; bulk/automatic kill;
  arbitrary commands or raw targets; deployment/production effects; commit,
  merge, push, publication; compatibility breaks; scope expansion; or unclear
  security/privacy impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Capability contract | completed | Opaque ID produces one separated kill invocation. | 23 infrastructure tests pass |
| 2. Protected API thin slice | completed | DELETE covers success and bounded negative paths. | 44 server integration tests pass |
| 3. Main-screen workflow | completed | Filter and confirmed menu action behave coherently. | Four frontend suites/typecheck pass |
| 4. Verification/docs/review | completed | Contracts and gates pass with review record. | Canonical verification passes; review ready |

## Progress

- 2026-08-14: owner approved detached-only filtering and ellipsis-menu session
  termination; capability classified T2 because termination is irreversible.
- 2026-08-14: scoped safeguards to one opaque inventory-resolved target, fixed
  tmux arguments, explicit named confirmation, auth/CSRF/rate limit/audit, and
  authoritative refresh; prohibited real-session destruction during testing.
- 2026-08-14: initial Change Review rejected the implementation after an
  isolated real-tmux probe showed last-session kill removes the target while
  returning `no server running`; execution resumed to reconcile authoritative
  post-command state and prevent false failure/retry.
- 2026-08-14: post-kill reconciliation now treats a missing target as applied
  success and preserves failure when the target still exists. Focused tests and
  fresh canonical verification pass; re-review is ready with explicit follow-ups.
- 2026-08-14: owner explicitly approved deployment to the existing Tailscale
  Serve app. Preflight, Compose config, healthy current service, exact-IP bind,
  and unchanged Serve mapping passed; execution resumed for gated rollout.
- 2026-08-14: preserved deployed image `sha256:73301f...` as
  `tmux-mobile:rollback-pre-c018-20260814`, built full C018 image
  `sha256:b1e0021...`, and passed the isolated host/container tmux 3.4 probe.
- 2026-08-14: recreated only the app container. It is healthy on the unchanged
  `100.85.13.102:8780` bind; Serve remains `:8443 -> :8780`, HTTPS liveness is
  200, direct backend is 426, anonymous DELETE is 401, and the image bundle
  contains the Detached/Kill controls. Existing clients reconnected.

## Evidence

- Infrastructure: 23 passed, four expected opt-in Linux PTY skips.
- Server integration: 44 passed, including protected success and bounded
  missing/CSRF/anonymous/rate-limit/tmux-failure paths.
- Frontend: typecheck and four unit suites pass; production Vite build passes
  and contains Detached/Kill controls.
- Canonical `PATH=/tmp/tmux-mobile-c018-dotnet:$PATH ./scripts/verify.sh`: passed.
- Isolated `tmux -L tmux-mobile-c018-review-20260814` proved last-session
  nonzero behavior without addressing or destroying the default tmux server.
- Live deployment: image `sha256:b1e0021...` healthy, exact-IP bind, tmux 3.4
  compatibility passed, HTTPS liveness 200, direct backend 426, protected DELETE
  401 anonymously, unchanged Serve route, rollback `sha256:73301f...` retained.

## Discoveries

- The UI currently filters only by name and already exposes `isAttached`.
- The current SPEC explicitly forbids destructive tmux capability, requiring the
  owner-approved contract revision before implementation.
- tmux 3.4 may remove its final session yet report `no server running`; a
  destructive API must reconcile authoritative absence before reporting failure.

## Decisions

- Detached is a filter criterion, not evidence that a session is abandoned.
- Kill remains available on the selected session's menu whether attached or
  detached; confirmation explains that the session and running programs end.
- Destructive DELETE requires the pre-existing Admin policy; all owner login
  identities already receive that permission.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: initial review found applied last-session termination could be
  misreported as failure because the tmux server exited before command success.

## Next Action

- Owner applies the PWA update, tests Detached filtering, and kills only a newly
  created disposable session; commit/merge/push remain separate boundaries.

## Pause Conditions

- Pause at every real-session destruction, deployment, commit/merge/push,
  unapproved command/target expansion, repeated unchanged failure, or unclear
  security/privacy boundary.

## Outcomes

- Local implementation, documentation, focused tests, production web build,
  canonical verification, isolated tmux evidence, Change Review, compatibility
  gate, and approved deployment are complete. The goal is paused before commit,
  merge, push, and physical-device acceptance.
