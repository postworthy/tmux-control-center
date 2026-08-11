# Goal: Live Session Search and Guarded Creation

Status: paused
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-11
Proposal: `PROPOSALS/2026-08-11--session-search-create.md`
Review Boundary: merge from `feat/c017-session-search-create` into `main`

## Outcome

The main screen filters its recency-ordered session deck live by session name,
and the authenticated owner can create one validated detached tmux session and
enter its terminal immediately with clear bounded failure feedback.

## Non-Goals

- No arbitrary commands, arguments, paths, environment, remote hosts, session
  destruction, fuzzy/content search, deployment, merge, push, or publication.

## Acceptance Criteria

- [x] AC1 — Every search edit immediately filters names case-insensitively;
  clearing restores all sessions in their recency order and no-match is explicit.
  - Evidence: pure filter tests cover partial/case/trim/clear/no-match/order;
    typecheck and source inspection confirm every deck consumer uses the same array.
- [x] AC2 — A valid unique name creates exactly one detached tmux session via a
  fixed argument-vector command and returns its opaque session ID.
  - Evidence: infrastructure tests assert the exact separated argument vector,
    normalized name, opaque ID, pre-launch rejection, duplicate, and malformed ID.
- [x] AC3 — Creation requires authorization, valid CSRF, rate-limit capacity,
  and valid input; duplicate and tmux failure paths are bounded and audited.
  - Evidence: 40 integration tests pass, including 201 success, inventory refresh,
    audit, 400 invalid/CSRF, 401 anonymous, 409 duplicate, and 503 tmux failure;
    endpoint retains the existing `interact` limiter.
- [x] AC4 — Successful creation refreshes inventory, records local recency, and
  opens the returned session terminal immediately; failure opens no terminal.
  - Evidence: source inspection confirms the create response is promoted and
    passed directly to terminal mode before a non-blocking inventory refresh;
    failures stay in the dialog and frontend typecheck passes.
- [x] AC5 — Existing API, terminal, recency, authentication, deployment, and
  tmux compatibility remain intact; documentation and canonical verification pass.
  - Evidence: API/architecture/README/SPEC agree; canonical verification passes
    through the documented .NET 10 container adapter; changed review is ready
    with explicit commit/deployment/physical follow-ups.

## Authority Envelope

### May Continue Without Asking

- The owner's explicit request authorizes local, reversible T0/T1 work and the
  scoped T2 capability addition described in the approved proposal: fixed-shape
  detached tmux session creation by validated name, protected by existing
  security controls, plus frontend search/create/open integration and tests.
- The owner separately approved deployment on 2026-08-11 to only the existing
  Tailscale Serve app, preserving its current image and state as rollback and
  leaving Serve, tailnet policy, secrets, and tmux sessions unchanged.
- The owner explicitly approved local commit, merge to `main`, and push of
  `main` to the existing GitHub `origin` on 2026-08-11 after healthy deployment.

### Must Pause for Approval

- Scope expansion to arbitrary commands, paths, environment, remote hosts,
  broader session management, destructive/irreversible actions, deployment,
  production effects, merge, push, publication, compatibility breaks, or any
  security/privacy uncertainty outside the approved fixed capability.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Capability contract | completed | Fixed-shape validated creation returns an opaque ID. | 18 infrastructure tests pass; four unrelated isolated tests skipped |
| 2. API thin slice | completed | Protected endpoint covers success and bounded negative paths. | 40 server integration tests pass |
| 3. Main-screen workflow | completed | Live filter and create/open behavior share coherent deck state. | Four frontend suites and typecheck pass |
| 4. Verification/docs/review | completed | Contracts agree and all required gates pass. | Canonical verification and changed Change Review pass |

## Progress

- 2026-08-11: owner approved live name filtering and create-then-open behavior.
- 2026-08-11: classified creation as scoped T2 because it narrows the existing
  no-process-launch boundary; recorded fixed argument shape and security controls.
- 2026-08-11: created `feat/c017-session-search-create` from clean `main`.
- 2026-08-11: implemented fixed-shape creation, opaque-ID response, inventory
  refresh, bounded errors, auditing, and protected API coverage.
- 2026-08-11: implemented the fixed top toolbar, live client-only name filter,
  coherent filtered deck/rail/navigation, explicit empty results, and a guarded
  create dialog whose success promotes and opens the returned target directly.
- 2026-08-11: host focused .NET invocation could not start because the host has
  SDK 8 while `global.json` requires 10.0.300. The repo-matched .NET 10 SDK and
  native compiler image built successfully and all .NET suites pass there.
- 2026-08-11: canonical `./scripts/verify.sh` passes using a temporary `dotnet`
  adapter to the local C017 test image; host setup, npm, and Compose gates run
  unchanged. Documentation is aligned; review remains.
- 2026-08-11: initial Change Review is not ready. Real isolated tmux evidence
  shows accepted `.` and `:` are silently rewritten to `_`, making the returned
  name non-authoritative and allowing normalized collisions. The correction is
  inside the approved fixed-name capability and execution resumes before review.
- 2026-08-11: added create-specific validation that rejects tmux-rewritten `.`
  and `:` before launch while preserving `a b_@+-` exactly. Fresh canonical
  verification passes with 27 Core, 18 Infrastructure, 40 Server integration,
  four frontend suites, setup, typecheck, and Compose gates. Changed review is
  ready with explicit follow-ups; execution pauses before commit/deployment.
- 2026-08-11: owner explicitly approved deployment. Preflight passes for tmux
  3.4, Docker Compose, and connected Tailscale. Existing app/Serve/container
  state was captured without reading secrets; execution resumes for rollout.
- 2026-08-11: preserved live C015 image `sha256:2bc9c5...` as
  `tmux-mobile:pre-c017-search-create-rollback`, built full C017 image
  `sha256:80f290...`, and passed the repository compatibility probe against an
  isolated socket with host/container tmux 3.4. Recreated only the existing app.
- 2026-08-11: deployed app is healthy at the unchanged exact bind
  `100.85.13.102:8780`; HTTPS liveness/root and internal readiness return 200,
  direct backend root returns 426, Serve remains `:8443 -> :8780`, and the live
  bundle contains Search sessions, Create & open, and `/api/sessions`. Startup
  logs show no errors and only the pre-existing acknowledged weak test-key warning.
- 2026-08-11: owner explicitly authorized committing C017, merging it to local
  `main`, and pushing `main` to the existing GitHub origin.
- 2026-08-11: committed the feature, contracts, tests, proposal, and product
  documentation as `416ac6b`; preparing the review/publication checkpoint.
- 2026-08-11: committed the review/deployment record as `3cf406e`, merged C017
  to `main` with merge commit `b7b8e5b`, and pushed `main` to the existing GitHub
  origin. Remote `main` was advanced from `137b774` to `b7b8e5b`.

## Evidence

- `docker run --rm tmux-mobile:c017-server-test dotnet test TmuxMobile.sln --no-restore`:
  27 Core, 18 Infrastructure passed with four expected isolated skips, and 40
  Server integration passed.
- `npm --prefix src/TmuxMobile.Web run typecheck`: passed.
- `npm --prefix src/TmuxMobile.Web run test:unit`: four suites passed.
- `PATH=/tmp/tmux-mobile-c017-dotnet:$PATH ./scripts/verify.sh`: passed in full;
  the adapter supplies the repo-required .NET 10 SDK absent from the host.
- `docker build --target server-build ...`: production frontend build and .NET
  publish passed as image `sha256:3829f7...` without deployment.
- isolated name probes: tmux rewrote `name.with:chars` to `name_with_chars`,
  while supported `a b_@+-` was retained exactly; both probe servers were killed.
- live deployment: image `sha256:80f290...`, healthy exact-IP binding, tmux 3.4,
  HTTPS liveness/root 200, readiness 200, direct backend 426, unchanged Serve,
  and live C017 bundle markers; rollback image `sha256:2bc9c5...` retained.

## Discoveries

- The current SPEC forbids process launch and exposes no create contract; the
  feature therefore requires an explicit narrow safety-boundary revision.
- Existing device-local recency and ordered-deck logic can remain authoritative
  before applying a client-only name filter.
- The existing global exception handler reports an antiforgery validation throw
  as 500 in the integration host. C017 catches that exception at its endpoint,
  audits failure, and returns bounded 400 without broadening scope to unrelated routes.
- tmux silently rewrites periods and colons in a newly created session name to
  underscores; rename's existing display grammar cannot be reused unchanged for
  a create response that promises the actual normalized session name.

## Decisions

- Accept only a session name; tmux selects its configured default shell and
  environment. Do not expose command-like options.
- Protect creation with Interact authorization, antiforgery, the existing
  interaction rate limiter, and audited success/failure.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: initial review found tmux name rewriting; create-specific
  validation changed the cause and the first corrective attempt passed.

## Next Action

- Owner force-closes/reopens or applies the PWA update, then tests live filtering and create-and-open on the physical device.

## Pause Conditions

- Pause at any scope expansion, destructive/production/external boundary,
  unclear target-validation or command-injection behavior, repeated unchanged
  failure beyond two attempts, or the merge/deploy/push boundary.

## Outcomes

- Local implementation, contracts, tests, production build, canonical gate,
  changed review, compatibility probe, and owner-approved deployment are
  complete. The live app is healthy with rollback preserved, and commits
  `416ac6b`/`3cf406e` are published on GitHub `main` through merge `b7b8e5b`.
  Physical acceptance remains pending.
