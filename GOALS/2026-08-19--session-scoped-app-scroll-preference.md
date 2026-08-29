# Goal: Persist App Scroll Per Session

Status: paused
Owner: Human Partner and AI Agent
Risk: T1
Updated: 2026-08-19
Proposal: `PROPOSALS/2026-08-19--session-scoped-app-scroll-preference.md`
Review Boundary: merge from a coherent C020 feature branch into `main`

## Outcome

An explicit App Scroll selection persists on this device across terminal exits,
reconnects, and reloads for exactly the tmux session where it was selected, while
all other never-enabled sessions remain default-off.

## Non-Goals

- No server/device synchronization, automatic application detection, API change,
  deployment, commit, merge, push, or publication.

## Acceptance Criteria

- [x] AC1 — Enabling session A persists through a fresh preference read while
  session B remains off; enabling B does not alter A.
  - Evidence: `npm --prefix src/TmuxMobile.Web run test:unit` passes the new
    session A/B persistence and isolation assertions.
- [x] AC2 — Disabling one session removes only its preference; malformed,
  unavailable, duplicate, and oversized storage is safe and bounded.
  - Evidence: the same focused suite passes disable isolation, strict parsing,
    exception fallback, deduplication, and a 128-ID cap.
- [x] AC3 — Terminal entry initializes the ref, pressed state, labels, and
  routing from the selected session's preference; exit and connection loss no
  longer clear it, and reconnect retains it.
  - Evidence: typecheck/build pass; lifecycle inspection finds initialization
    and explicit-toggle write with no reset call on mount, close, or back; built
    terminal chunk contains the persisted key and App Scroll control.
- [x] AC4 — Existing application-wheel coalescing, velocity/distance routing,
  Older/Latest dual routing, focus neutrality, keyboard behavior, and
  default-off tmux history remain compatible.
  - Evidence: canonical verification passes 27 Core, 23 Infrastructure plus
    four expected skips, 44 server integration, and five frontend suites.
- [x] AC5 — Product/architecture docs and Change Review describe device-local,
  session-only persistence and rollback accurately.
  - Evidence: README, SPEC, roadmap, architecture, proposal, and
    `REVIEWS/2026-08-19--session-scoped-app-scroll-preference.md` agree.

## Authority Envelope

### May Continue Without Asking

- The owner's 2026-08-19 request approves local reversible T1 implementation,
  tests, docs, and review for this exact session-scoped persistence behavior.
- Add one bounded device-local preference key; use disposable test storage only.

### Must Pause for Approval

- Server-side/cross-device persistence, raw session-name keys, automatic mode
  detection, API changes, destructive or production action, deployment, commit,
  merge, push, publication, compatibility breaks, or unclear security/privacy.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Storage thin slice | completed | Isolation, persistence, disable, bounds, fallback pass. | Five frontend suites pass |
| 2. Terminal lifecycle | completed | Entry reads; explicit toggle writes; exit/disconnect preserve. | Typecheck/build + inspection pass |
| 3. Contracts/review | completed | Docs, canonical gate, and review evidence complete. | Canonical pass + Review Record ready |

## Progress

- 2026-08-19: owner explicitly superseded the non-persistent lifecycle for App
  Scroll and requested device-local persistence scoped to the enabled session.
- 2026-08-19: preflight found the existing C018 worktree dirty but no overlap in
  `TerminalView` or new preference/test files; overlapping SPEC/roadmap edits
  will be patched narrowly without replacing existing work.
- 2026-08-19: added a pure guarded preference helper and regression suite. All
  five frontend unit suites pass; terminal lifecycle wiring is implemented and
  awaiting typecheck/build verification.
- 2026-08-19: typecheck and production build pass. Generated terminal code
  contains the persisted key; mount, WebSocket close, and back navigation no
  longer clear the preference.
- 2026-08-19: direct canonical verification first failed because host SDK 8.0.130
  cannot satisfy pinned 10.0.300. The changed adapter cause was corrected by
  using the established local .NET 10.0.302 SDK; the full gate then passed.
- 2026-08-19: Change Review is ready with explicit physical acceptance,
  deployment approval, and coherent-git-integration follow-ups. Execution pauses
  at those owner-controlled boundaries.
- 2026-08-19: owner explicitly approved deployment. Production execution resumes
  through the existing image rollback, isolated tmux probe, and Tailscale Serve
  verification gates; commit, merge, and push remain unapproved.
- 2026-08-19: deployment preflight passed and preserved running image
  `sha256:b1e0021fcc1f96aca73a1f6417d903dc4a5c3bc12590ea5f19d61c241aa9db84`
  as `tmux-mobile:rollback-pre-c020-20260819`. The first build invocation was
  rejected because the managed sandbox cannot update Docker Buildx activity
  state under `/home/landon/.docker`; retry uses the same command with the
  required host permission, so the failure cause and intervention differ.
- 2026-08-19: built image
  `sha256:60d2e566dbf0cd2ccd22d75e775e0dba1341a1992ef53b2d70b722e19255c602`,
  proved host/container tmux 3.4 communication on an isolated disposable
  socket, and deployed only the app container. The replacement became healthy
  with zero restarts and retained the host tmux inventory.
- 2026-08-19: post-deploy verification observed the exact Tailscale-IP bind,
  unchanged Serve `:8443` mapping, HTTPS root/liveness 200, anonymous API 401,
  direct backend 426, and the persisted preference marker in the live terminal
  bundle. Execution pauses for the owner's physical session A/B matrix.

## Evidence

- `npm --prefix src/TmuxMobile.Web run test:unit`: five suites passed.
- `npm --prefix src/TmuxMobile.Web run typecheck`: passed.
- `npm --prefix src/TmuxMobile.Web run build`: passed; service-worker release
  `c02de5582fead907` stamped.
- `./scripts/verify.sh`: expected SDK-boundary failure after setup tests; host
  exposes only SDK 8.0.130.
- `PATH=/tmp/tmux-dotnet10:$PATH DOTNET_ROOT=/tmp/tmux-dotnet10 ./scripts/verify.sh`:
  passed all .NET and frontend gates.
- Change Review: ready with explicit follow-ups.
- `./scripts/first-run-setup.sh probe-tmux`: passed with tmux 3.4 on isolated
  socket `tmux-mobile-probe-2-56aa4aab`.
- Production image: healthy
  `sha256:60d2e566dbf0cd2ccd22d75e775e0dba1341a1992ef53b2d70b722e19255c602`
  with zero restarts; rollback tag
  `tmux-mobile:rollback-pre-c020-20260819` resolves to the prior image.
- Live checks: HTTPS root/liveness `200`, anonymous `/api/sessions` `401`,
  direct backend root `426`, exact listener `100.85.13.102:8780`, and live
  bundle contains `tmux-mobile-application-scroll-sessions`.

## Discoveries

- Pre-change code explicitly reset App Scroll on terminal mount, disconnect,
  and back navigation.
- Existing device-local recency establishes guarded `localStorage` as the app's
  preference boundary.

## Decisions

- Store only a bounded unique set of enabled opaque session IDs. Absence means
  off, keeping new sessions default-off.
- Write only when the owner explicitly toggles; connection lifecycle does not
  mutate the preference.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: resolved by granting the approved Docker build access required
  to update Buildx's user-level activity metadata; the unchanged build then
  passed.

## Next Action

- Owner performs the physical matrix: enable App Scroll in session A, exit and
  reopen A, force reconnect A, confirm A remains enabled, open never-enabled
  session B and confirm it remains off, then disable A and reopen it to confirm
  the explicit off state persists.

## Pause Conditions

- Pause at any Authority Envelope boundary, overlapping non-C020 user edit,
  unsafe storage requirement, or third unchanged failure.

## Outcomes

- Local implementation, docs, focused tests, canonical verification, Change
  Review, rollback preparation, and production deployment are complete. The
  live service is healthy; physical acceptance and git integration remain
  separate owner-controlled boundaries.
