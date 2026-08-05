# Goal: Session Tiles Ordered by In-App Recency

Status: paused
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-05
Proposal: `PROPOSALS/2026-08-04--session-tile-recency.md`
Review Boundary: merge from `feat/c013-session-recency` into `main`

## Outcome

Opening a session terminal from the main deck records device-local recency, and
returning renders that session as the first tile with all deck navigation and
rail state using the same stable order.

## Non-Goals

- Do not infer recency from tmux activity or external clients, synchronize
  devices, add manual ordering, or change backend/tmux contracts.
- Do not merge, push, publish, or modify C012 acceptance in this goal. Deployment
  is limited to the separately owner-approved existing tailnet test app.

## Acceptance Criteria

- [x] AC1 — Terminal open promotes one opaque session ID to the MRU front without
  duplicates and subsequent opens produce deterministic newest-first order.
  - Evidence: pure frontend promotion tests pass.
- [ ] AC2 — Returning from terminal renders the opened session as tile 1 and all
  tile, navigation, selection, terminal lookup, and rail consumers agree.
  - Evidence: source inspection and typecheck pass; owner acceptance pending.
- [x] AC3 — Valid recency persists across inventory replacement and reload;
  malformed/stale IDs fail safely and untouched/new sessions retain server order.
  - Evidence: parsing, persistence, pruning, stable ordering, and unavailable
    storage tests pass.
- [x] AC4 — Backend, WebSocket, tmux, authentication, and network contracts are
  unchanged; canonical verification and documentation pass.
  - Evidence: diff review, docs, production image, and canonical gate pass.

## Authority Envelope

### May Continue Without Asking

- Approved, local, reversible T0/T1 edits, tests, builds, documentation, commits,
  and review artifacts required by C013.
- Owner-approved deployment of the verified C013 image to the existing tailnet
  test app, preserving the current image as a rollback tag and repeating its
  established security/health checks.

### Must Pause for Approval

- Scope expansion, deployment, merge, push, publication, server persistence,
  cross-device synchronization, backend/API/tmux changes, destructive actions,
  compatibility breaks, or unclear security/privacy impact.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Ordering contract | completed | Pure helpers safely parse, promote, prune, and stably order IDs. | Frontend unit tests passed |
| 2. Deck thin slice | completed | Opening and returning promotes tile 1; every order consumer agrees. | Typecheck and source inspection passed |
| 3. Verification/docs | completed | Canonical gate, docs, rollback, and review agree. | `./scripts/verify.sh` and image build passed |

## Progress

- 2026-08-04: owner approved device-local recency ordering by opening a terminal
  from the main deck; C012 remains separately paused for physical acceptance.
- 2026-08-04: created `feat/c013-session-recency` from the verified C012 branch.
- 2026-08-04: implemented safe device-local parsing/writes, duplicate-free MRU
  promotion, stale-ID pruning, stable ordering, and consistent deck integration.
  Focused unit tests and typecheck pass.
- 2026-08-04: canonical verification and production image
  `sha256:e1e79f...` pass. The clean runtime bundle contains the recency key and
  worker release `4fc71288e076c290`; execution pauses before the unapproved
  deployment boundary and owner acceptance.
- 2026-08-04: committed the verified C013 implementation and contracts as
  `8032623`.
- 2026-08-05: owner explicitly approved deployment. Preserved live velocity
  image `sha256:035f7f...` as `tmux-mobile:pre-c013-recency-rollback`, tagged
  verified C013 image `sha256:e1e79f...` as
  `tmux-mobile:c013-session-recency`, and recreated only the existing app.
  Post-deployment checks pass; execution pauses for physical acceptance.

## Evidence

- Frontend unit suite passes three files, including C013 promotion, ordering,
  persistence, malformed/unavailable storage, immutability, and stale-ID cases.
- Frontend typecheck passes.
- Diff inspection confirms no server, API, WebSocket, tmux, authentication, or
  deployment code changed.
- Canonical `./scripts/verify.sh` passes with 24 Core, 12 Infrastructure (four
  isolated tests skipped), 33 Server integration, all three frontend suites,
  and Compose validation.
- Production image `sha256:e1e79f...` builds with only the current main and
  terminal bundles; the main bundle contains `tmux-mobile-session-recency` and
  the worker is stamped `tmux-mobile-shell-4fc71288e076c290`.
- Live C013 deployment is healthy on unchanged exact bind
  `100.85.13.102:8780`; HTTPS liveness/root return 200, direct backend root
  returns 426, internal readiness returns Healthy, and tmux enumerates all 13
  current sessions. Root references `index-DD9u1j3o.js`, whose live bytes contain
  `tmux-mobile-session-recency`; the live terminal bundle still contains App
  Scroll and the worker advertises `tmux-mobile-shell-4fc71288e076c290`.
- Startup logs contain no new errors and only the existing explicitly
  acknowledged weak test-key warning.

## Discoveries

- App currently renders, navigates, and calculates rail position directly from
  server snapshot order.
- The deck is absent while terminal mode renders, so the promoted first tile
  naturally mounts at scroll position zero on return.
- Existing local storage already retains one opaque active-session ID; C013 adds
  only an ordered list of opaque IDs and no session content.

## Decisions

- Define recency only as Terminal actions initiated inside this app.
- Persist MRU IDs locally on the device; rank valid IDs first and append all
  unranked live sessions in unchanged server order.
- Derive every presentation/navigation consumer from one ordered array.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- Owner applies/reloads the PWA update, opens a non-first session terminal, and
  confirms that returning places it first and that the order survives reload.

## Pause Conditions

- Pause if correct ordering requires backend state, session content persistence,
  external-client observation, or a compatibility/security boundary.

## Outcomes

- Local implementation, tests, documentation, production packaging, and
  canonical verification are complete. The verified image is healthy in the
  existing tailnet test environment with the prior image retained for rollback.
  Physical acceptance, review decision, merge, and push remain pending.
