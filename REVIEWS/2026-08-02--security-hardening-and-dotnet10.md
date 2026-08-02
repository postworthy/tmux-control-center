# Review Record: Security Hardening and .NET 10 LTS Migration

Date: 2026-08-02
Review Boundary: merge from `security/c011-dotnet10-and-hardening` into `main`
Merge Method: `git merge --no-ff security/c011-dotnet10-and-hardening`
Risk Class: T3
Related Proposal: `PROPOSALS/2026-08-02--security-hardening-and-dotnet10.md`

## Decision

Ready with explicit follow-ups. The approved findings 2-10 are implemented,
verified, documented, and actively deployed. There are no blocking code,
security, compatibility, secret-hygiene, or recovery findings. The temporary
access key remains unchanged by explicit scope, physical-iPhone regression
acceptance remains the paused C009 follow-up, and merge/push still require the
owner's separate approval.

## Branch and Commits

- Source: `security/c011-dotnet10-and-hardening`
- Target: `main` at `774217e`
- `5b304d6` chore(runtime): migrate to .NET 10 LTS
- `7ee115c` fix(security): fail closed on unsafe production configuration
- `424cd90` fix(security): partition traffic and health limits
- `0aab695` fix(terminal): guarantee PTY process-group cleanup
- `651aea3` fix(audit): harden interaction audit integrity
- `6552da6` fix(security): harden browser and proxy boundary
- `c486103` docs(security): record verified deployment posture

The review/goal closure commit is documentation-only evidence after these
implementation commits.

## Git and Scope

- [x] Work is on the approved C011 feature branch, not `main`.
- [x] Commit subjects are conventional and each commit carries the required
  `Roadmap` and `Proposal` trailers.
- [x] The diff matches the approved runtime, security, test, deployment, and
  documentation decomposition.
- [x] No API route/schema, destructive action, arbitrary execution, remote-host,
  public-ingress, or unrelated feature entered scope.
- [x] `.env`, audit/state, keys, certificates, logs, package caches, and the
  generated native `.so` are absent from tracked changes.
- [x] Gitleaks 8.30.1 reports no leaks across all nine reachable commits; a
  separate pending-diff scan was also clean before its commit.

## Acceptance Evidence

- [x] AC1: every project targets `net10.0`; SDK 10.0.302 restore/build/test and
  Release publish pass; the live image reports ASP.NET/Core 10.0.10. NuGet and
  npm audits report zero vulnerabilities.
- [x] AC2/AC6: disabled/development production auth, legacy bypass, unsafe test
  switches, malformed/insecure origins, Host mismatches, HTTP listener posture,
  and untrusted forwarding combinations fail closed in focused startup tests.
- [x] AC3: the native fork-and-immediate-exec boundary owns a distinct PTY
  process group, escalates HUP/TERM/KILL, and reaps its leader. All three
  isolated Linux tests pass, including stubborn descendants and tmux-session
  survival; no ordinary tmux socket is addressed.
- [x] AC4: success and allowed failures produce content-free records; Linux
  audit storage is owner-only; injected sink failure does not turn an applied
  rename into a retryable action failure. The live audit file is `0600`, has
  only timestamp/action/subject/target/succeeded fields, and contains no access
  key.
- [x] AC5/AC8: authentication precedes identity-aware global/interaction
  limiting; login, health, terminal connection, terminal message, and byte
  boundaries are independent. Anonymous liveness is cheap; readiness is
  loopback-or-authorized and succeeds locally in the deployed container.
- [x] AC7: live HTTPS emits `Strict-Transport-Security: max-age=31536000`, CSP
  limits connections to `'self'`, APIs are `no-store`, and the built shell plus
  manifest/service worker/icons/main and terminal bundles return 200.
- [x] AC9: Docker listens only on `100.85.13.102:8780`; the existing tailnet-only
  Serve origin `https://ubuntu-box-1.monster-ionian.ts.net:8443/` returns 200;
  direct backend application HTTP with the accepted Host returns 426. Liveness
  and local readiness remain healthy.
- [x] AC10: canonical backend/frontend checks cover existing REST/WebSocket,
  validation, clipboard chunking, scrollback control, reconnect support, PTY,
  and PWA shell behavior. Live login with the unchanged key returns 204 and
  authenticated status returns 200 without reading or writing user pane data.
- [x] AC11: README, architecture, API, configuration, security, deployment,
  Docker/Tailscale guidance, decisions, roadmap, status, rollback, and this
  review agree with implementation and live evidence.

## Verification

```bash
PATH=/tmp/tmux-dotnet10:$PATH \
DOTNET_CLI_HOME=/tmp/tmux-dotnet-cli \
NUGET_PACKAGES=/tmp/tmux-nuget10 \
./scripts/verify.sh
```

- Pass: 24 Core, 12 Infrastructure, and 33 Server tests; three opt-in Linux
  tests skipped in the canonical run; frontend typecheck and both unit suites
  pass.

```bash
TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 dotnet test \
  tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj
dotnet list TmuxMobile.sln package --vulnerable --include-transitive
npm --prefix src/TmuxMobile.Web audit --audit-level=moderate
dotnet publish src/TmuxMobile.Server/TmuxMobile.Server.csproj -c Release
docker compose ... config
gitleaks detect --source=. --redact
```

- Pass: all 15 Infrastructure tests including three isolated Linux tests; no
  vulnerable NuGet/npm packages; publish and all three Compose profiles pass;
  Gitleaks scans nine commits and finds no leaks.
- Production image build and two staged container replacements pass. The first
  live check discovered local readiness was over-restricted; `c486103` exempts
  only loopback readiness, focused/canonical tests pass, and the final deployed
  container is healthy.

## Findings and Risk

- Blocking/high/medium unresolved findings: none.
- Resolved low: the native build output appeared as an untracked `.so` beside
  the project. `.gitignore` now excludes that exact generated path.
- Intentional risk: the owner explicitly excluded access-key replacement. The
  weak-key test acknowledgement remains visible in startup logs and
  configuration; application auth, Secure cookies, CSRF, rate limits, and the
  tailnet-only HTTPS origin remain active.
- Low follow-up: no tailnet policy was modified. The server-side 426 boundary
  prevents ordinary direct browser use; operators should continue to omit 8780
  from tailnet grants and limit 8443 to the owner/device.
- Device follow-up: this review did not repeat installed-iPhone sleep/wake,
  orientation, and network-change checks. That subjective/device evidence is
  retained under paused C009 and does not weaken the C011 server hardening.

## Compatibility and Recovery

- REST/WebSocket routes and schemas, safe identifiers, tmux ownership, PWA
  navigation, clipboard, history controls, and persistent state are unchanged.
- Active browser terminals disconnect during container replacement, while PTY
  cleanup leaves tmux sessions running. No persistent data migration exists.
- Pre-rollout image `sha256:1e84c9d3fdf0a097e61e1b7ef70eb130425f09c07a0d858aa2295a4ca29b347f`
  is tagged locally as `tmux-mobile:pre-c011-rollback`.
- Rollback: point the Serve Compose image tag at that image, recreate only the
  app, retain key/audit/Data Protection/tmux mounts, then verify container
  health, HTTPS root/login, local readiness, listener addresses, logs, and the
  existing Serve rule. Do not roll back through disabled auth, wildcard bind,
  insecure origin, or deletion of state.

## Approvals

- Proposal implementation and active replacement of the existing tmux-mobile
  deployment: repository owner, 2026-08-02.
- Reviewer: Codex, evidence-backed local and live review.
- Review status: ready with explicit follow-ups.
- Merge/push/publication: pending owner approval; not performed or implied.

## Follow-Ups

- Owner may validate the installed PWA on the physical iPhone and close paused
  C009 device acceptance.
- Replace the deliberately temporary access key when the owner chooses; this
  remains outside C011.
- Confirm tailnet grants/ACLs expose 8443 only to the intended identity/device
  and do not grant 8780 to ordinary clients.
