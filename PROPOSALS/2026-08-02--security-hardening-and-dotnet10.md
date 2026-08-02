# Proposal: Security Hardening and .NET 10 LTS Migration

Date: 2026-08-02
Owner: Human Partner and AI Agent
Risk Class: T3
Related Issue/Context: 2026-08-02 read-only security review, findings 2-10
Roadmap Item: C011
Planned Branch: `security/c011-dotnet10-and-hardening`
Expected Commit Count: 7

## Objective

Move the application to the latest supported .NET LTS release first, then close
the remaining authentication, PTY lifecycle, audit-integrity, rate-limit,
deployment, configuration, browser-policy, and readiness gaps identified by the
security review without changing the HTTP/WebSocket product contracts or the
observation-first mobile workflow.

At proposal time, .NET 10 is the latest LTS release and is supported through
2028-11-14 according to the official .NET support policy:
<https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core>.

## Scope

In scope:

- Migrate every application and test project from .NET 8 to .NET 10, update the
  SDK/runtime container stages and compatible test/framework packages, and
  prove clean restore, build, test, publish, image build, and vulnerability
  audit behavior before beginning other hardening.
- Remove the production authentication-disable escape hatch. Development
  authentication may run only in the Development environment with its explicit
  local bypass setting; Production must always require a real authentication
  mode.
- Make terminal PTY cleanup process-group aware and bounded, escalating through
  graceful termination to a final forced kill while preserving the underlying
  tmux session and reaping the client process.
- Make action auditing complete and operationally unambiguous: record allowed
  failed attempts, distinguish action outcome from audit-write outcome, avoid
  reporting an already-applied action as failed solely because its audit append
  failed, validate protected audit-file permissions, and document rotation and
  failure handling without recording terminal contents or keystrokes.
- Correct rate-limit ordering and partitioning so authenticated traffic can use
  identity-aware limits, login attempts remain independently bounded, proxy
  clients cannot trivially exhaust one shared interaction bucket, and terminal
  input receives a bounded message/byte throughput policy in addition to its
  existing frame, idle, and connection limits.
- Strengthen startup validation for exact absolute origins, HTTPS production
  posture, Host restrictions, forwarded-header trust, unsafe switches, and
  mutually dependent deployment settings. Unsafe test combinations must fail
  closed outside narrowly documented test profiles.
- Add an explicit production HSTS policy at the HTTPS-facing layer, narrow CSP
  WebSocket destinations to approved application origins, document any xterm.js
  inline-style exception, and emit server-side `Cache-Control: no-store` for
  authenticated API responses.
- Restrict process-executing readiness checks to a local or explicitly
  authorized operational path while retaining an inexpensive anonymous
  liveness check.
- Preserve the exact Tailscale-IP host-publish invariant from D005 while making
  the Tailscale Serve backend port non-client-accessible through documented and
  verified Tailscale grants/ACLs or equivalent host filtering. Any live policy
  or firewall modification remains a separately approved T3 action.
- Add focused unit and integration coverage, operational documentation,
  architecture/decision updates, canonical verification, dependency audits,
  container checks, and a Tempo Change Review.

Out of scope:

- Changing or rotating the currently approved temporary access key. Security
  review finding 1 is deliberately excluded by the owner.
- Adding OAuth, OIDC, Tailscale identity-header authentication, multiple users,
  destructive tmux actions, arbitrary commands, filesystem access, or remote
  hosts.
- Changing REST or WebSocket route names, request/response schemas, terminal
  interaction semantics, PWA navigation, or tmux authority.
- Binding Docker to a wildcard or LAN address, removing application-level
  authentication, or treating Tailscale as the sole authorization layer.
- Performing a production deployment, editing tailnet policy, rotating secrets,
  modifying host firewall rules, merging, or pushing without a later explicit
  approval at the applicable boundary.

## Expected Files Touched

- `Dockerfile`
- `compose.yaml`
- `compose.tailscale-serve.yaml`
- `compose.http-test.yaml`
- `src/TmuxMobile.Server/TmuxMobile.Server.csproj`
- `src/TmuxMobile.Server/Program.cs`
- `src/TmuxMobile.Server/ConfigurationValidation.cs`
- `src/TmuxMobile.Server/WebSocketHandlers.cs`
- `src/TmuxMobile.Core/Options.cs`
- `src/TmuxMobile.Core/TmuxMobile.Core.csproj`
- `src/TmuxMobile.Infrastructure/TmuxMobile.Infrastructure.csproj`
- `src/TmuxMobile.Infrastructure/LinuxPseudoTerminal.cs`
- `src/TmuxMobile.Infrastructure/Inventory.cs`
- `tests/**/*.csproj`
- `tests/TmuxMobile.Core.Tests/*`
- `tests/TmuxMobile.Infrastructure.Tests/*`
- `tests/TmuxMobile.Server.IntegrationTests/*`
- `deploy/nginx/tmux-mobile.conf`
- `deploy/systemd/*`
- `deploy/docker/*`
- `docs/architecture.md`
- `docs/configuration.md`
- `docs/deployment.md`
- `docs/security.md`
- `docs/api.md`
- `README.md`
- `DECISIONS.md`
- `ROADMAP/COMMIT-PLAN.md`
- `STATUS.md`

## Acceptance Criteria

- [ ] AC1 — All application and test projects target .NET 10, production images
  use .NET 10 SDK/runtime bases, clean restore/build/test/publish and container
  build pass, and current NuGet/npm vulnerability checks report no known
  vulnerabilities before later units start.
- [ ] AC2 — Production startup cannot activate disabled or development
  authentication under any override combination; regression tests prove the
  application fails closed.
- [ ] AC3 — Disconnect, timeout, startup failure, and application shutdown leave
  no PTY client or descendant process while a dedicated isolated tmux session
  remains alive; tests never touch the owner's ordinary tmux server.
- [ ] AC4 — Successful and allowed failed interaction attempts have audit
  evidence, terminal content remains absent, audit storage permissions are
  checked/documented, and an injected audit-write failure cannot turn an
  already-applied action into a misleading retryable failure.
- [ ] AC5 — Login, authenticated HTTP interaction, terminal connections, terminal
  input throughput, and anonymous health traffic have explicit independently
  tested partitions; middleware ordering permits identity-aware limits after
  authentication.
- [ ] AC6 — Production startup rejects wildcard, malformed, non-origin, or
  insecure origin values; unsafe auth/HTTP/forwarding/Host combinations fail
  closed with actionable errors.
- [ ] AC7 — HTTPS responses expose an approved HSTS policy, CSP permits only the
  required application WebSocket destinations, authenticated API responses are
  marked `no-store`, and xterm.js/PWA behavior still works under the resulting
  headers.
- [ ] AC8 — Anonymous liveness remains inexpensive, while the tmux-executing
  readiness path rejects untrusted remote callers and remains usable by local
  container/systemd health checks.
- [ ] AC9 — The Compose host mapping remains bound only to the configured
  Tailscale IP, and deployment guidance plus staged evidence demonstrate that
  ordinary tailnet clients use the HTTPS Serve origin and cannot directly reach
  the backend HTTP port after the separately approved policy rollout.
- [ ] AC10 — Existing REST/WebSocket contracts, tmux identifier validation,
  mobile overview, clipboard, scrollback, reconnect, and PTY-backed terminal
  behavior remain compatible; canonical verification passes.
- [ ] AC11 — Architecture, security, configuration, deployment, API, rollback,
  operational, roadmap, status, and durable-decision documentation agree with
  the verified implementation, and a Change Review finds no secret or generated
  runtime material in the diff or reachable history.

## Verification Plan

Commands:

```bash
./scripts/verify.sh
dotnet list /home/landon/code/tmux-control-center/TmuxMobile.sln package --vulnerable --include-transitive
npm --prefix src/TmuxMobile.Web audit --audit-level=moderate
docker compose --env-file deploy/docker/.env.example config
docker compose --env-file deploy/docker/.env.example build
```

Focused checks will include:

```bash
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj
dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
TMUX_MOBILE_RUN_REAL_TMUX_TESTS=1 dotnet test tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
```

Pass means:

- Each acceptance criterion has named observable evidence.
- .NET 10 restore, compilation, test, publish, runtime startup, and container
  execution succeed without a compatibility fallback to .NET 8.
- Security-negative tests demonstrate fail-closed behavior, not merely valid
  configuration success.
- Isolated PTY tests prove process cleanup and tmux-session survival.
- Canonical verification exits 0 and dependency audits report no known
  vulnerabilities from their configured current sources.
- Any live Tailscale/production evidence is gathered only after the owner grants
  the separate T3 approval.

## Change Review Plan

- Review Boundary: merge from `security/c011-dotnet10-and-hardening` into `main`
- Planned Review Record:
  `REVIEWS/2026-08-02--security-hardening-and-dotnet10.md`
- Reviewer/approver expectation: repository owner confirms criteria, rollback,
  compatibility, secret hygiene, and any separately approved production policy
  evidence before merge.

## Git Plan

- Branch command:
  `git switch -c security/c011-dotnet10-and-hardening main`
- Commit subject pattern: `chore(runtime):`, `fix(security):`,
  `fix(terminal):`, `fix(audit):`, `test(security):`, and `docs(security):`
- Required commit trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#C011`
  - `Proposal: PROPOSALS/2026-08-02--security-hardening-and-dotnet10.md`
- Planned merge method:
  `git merge --no-ff security/c011-dotnet10-and-hardening`

## Decomposition Plan (Required for T1/T2/T3)

Work units (ordered):

1. .NET 10 LTS foundation — Verify with clean restore, focused and canonical
   tests, publish, container build/start, and dependency audits — Exit criteria:
   AC1 passes and no remaining project/image targets .NET 8 — Risk: T2 —
   Dependencies: .NET 10 SDK and compatible container images/packages.
2. Fail-closed authentication and configuration — Verify with a configuration
   matrix of valid production, valid development, and invalid unsafe/origin/
   Host/proxy combinations — Exit criteria: AC2 and AC6 pass — Risk: T2 —
   Dependencies: unit 1.
3. Identity-aware traffic and health boundaries — Verify with integration tests
   for login, authenticated partitions, proxy behavior, WebSocket/input bursts,
   liveness, and readiness callers — Exit criteria: AC5 and AC8 pass — Risk: T2
   — Dependencies: unit 2.
4. Guaranteed PTY lifecycle — Verify with fakes plus a dedicated-socket real
   tmux test covering graceful, stubborn-child, disconnect, and shutdown paths
   — Exit criteria: AC3 passes — Risk: T2 — Dependencies: unit 1.
5. Audit integrity and failure semantics — Verify with injected action/audit
   outcomes, permission checks, log-content assertions, and retry-safety tests —
   Exit criteria: AC4 passes — Risk: T2 — Dependencies: units 2 and 3.
6. Browser and deployment hardening — Verify headers against the built app,
   exercise xterm/PWA loading, validate all Compose/proxy/systemd profiles, and
   document the exact Tailscale policy change and rollback — Exit criteria: AC7
   and the repository-local portion of AC9 pass — Risk: T2 — Dependencies:
   units 2 and 3.
7. Integration, documentation, and review — Verify canonical behavior,
   dependency audits, image configuration, secret hygiene, documentation
   agreement, and rollback rehearsal; collect live AC9 evidence only if the
   owner separately authorizes the production/tailnet phase — Exit criteria:
   AC9-AC11 pass and the Review Record is merge-ready — Risk: T3 — Dependencies:
   units 1-6 and explicit production approval.

Thin slice milestone:

- Unit 1 produces a runnable, tested, containerized .NET 10 version with no
  intentional behavior change. This is the required first milestone and review
  checkpoint before any other security behavior changes begin.

Dependencies and unknowns:

- Confirm the development host and Docker builder can obtain the current .NET
  10 SDK/runtime images and that all NuGet dependencies support `net10.0`.
- Confirm xterm.js requires the existing inline-style CSP exception in the
  built application; do not weaken `script-src` to solve a style problem.
- Select and test the precise process-group/session strategy supported safely by
  the Linux `forkpty` implementation.
- Define the expected action response when audit storage fails without hiding
  the operational fault or encouraging duplicate action retries.
- Determine the exact Tailscale grants/ACL expression using the installed
  Tailscale version and current tailnet model; Tailscale syntax and policy
  changes are external T3 state.
- Decide whether HSTS is emitted by ASP.NET Core, the terminating proxy, or both,
  while ensuring the browser-facing HTTPS response is covered.

Intentional deferrals:

- Access-key replacement and removal of the owner's temporary credential are
  excluded from C011.
- A new identity provider, read-only accounts, admin UI, destructive actions,
  and public ingress remain future work.
- Supply-chain signing, SBOM attestation, and automated container registry
  publication are not required for this hardening goal.

## Rollback Plan

If this change causes regressions:

1. Revert C011 commits in reverse dependency order; do not rewrite shared
   history.
2. Rebuild the prior reviewed .NET 8 image from the pre-C011 commit and restore
   the previous Compose definition and application configuration.
3. If a separately approved Tailscale/host policy change occurred, restore the
   captured prior policy through the owner-controlled rollback procedure.
4. Validate rollback with `./scripts/verify.sh`, the prior image health check,
   authenticated inventory, a dedicated-session terminal lifecycle check, and
   exact listener inspection.
5. Do not roll back by enabling disabled authentication, wildcard binding,
   wildcard origins, or insecure cookies.

## Risks and Mitigations

- Risk: Combining runtime migration with security changes obscures regression
  causes.
  Mitigation: finish and checkpoint the behavior-neutral .NET 10 thin slice
  before changing security behavior.
- Risk: PTY process-group termination could affect the underlying tmux server.
  Mitigation: signal only the newly created attach-client process group and
  prove the isolated tmux server/session survives every cleanup test.
- Risk: Audit error handling could silently lose required records.
  Mitigation: make audit degradation observable, test it explicitly, preserve
  content redaction, and document operator response and rotation.
- Risk: New rate limits could lock out the owner or impair paste/terminal use.
  Mitigation: partition by identity/connection, use bounded bursts, test the
  128-KiB guarded paste path, and expose clear 429/close behavior.
- Risk: CSP, HSTS, Host, origin, or proxy validation could break the installed
  PWA or WebSockets behind Tailscale Serve.
  Mitigation: test the real browser-facing origin and preserve an explicit
  rollback before production rollout.
- Risk: Direct-backend restriction could make health checks or Tailscale Serve
  unable to reach the container.
  Mitigation: prove proxy and local health paths before applying client-deny
  policy, then verify both allowed and denied paths.
- Risk: Current deployment remains on the old security posture while C011 is in
  progress.
  Mitigation: do not claim production remediation until separately approved
  rollout evidence exists; keep repository and production status distinct.

## Compatibility / Migration Notes

- API compatibility impact: none intended; existing REST and WebSocket clients
  continue using the same routes and payloads.
- Data/schema migration needed: no. Data Protection keys and audit logs remain
  external persistent state and must be preserved across the runtime upgrade.
- Runtime migration: .NET 10 is a one-way build/runtime requirement for C011,
  but rollback retains the prior .NET 8 image and commit until review and live
  validation complete.
- Deployment migration: repository-local Compose/proxy/systemd changes precede
  any owner-approved tailnet or host-policy cutover.
- Backward compatibility window: retain the last reviewed .NET 8 image and its
  configuration until .NET 10 terminal, sleep/wake, reconnection, health, and
  rollback checks pass.

## Observability / Debug Notes

- Add structured events for authentication configuration rejection, rate-limit
  rejection category, readiness denial, PTY signal escalation/reap outcome, and
  audit sink degradation without logging secrets, terminal input, or output.
- Detect failure quickly through startup validation, health state, 401/403/429
  rates, PTY cleanup warnings, audit degradation events, orphan-process checks,
  WebSocket lifecycle logs, and exact listener inspection.
- Keep audit records and ordinary operational logs separate; neither may contain
  access keys or terminal bytes.

## Approval

- Proposal drafting: requested and approved by the repository owner on
  2026-08-02.
- Implementation and active deployment: approved by the repository owner on
  2026-08-02 with the instruction to continue through completion and active
  deployment.
- The approved deployment scope includes the existing tmux-mobile container and
  Tailscale Serve path. Preserve rollback evidence before any tailnet grant/ACL
  or host-filtering change. Secret rotation, merge, and push remain separate
  boundaries; finding 1 remains excluded.
