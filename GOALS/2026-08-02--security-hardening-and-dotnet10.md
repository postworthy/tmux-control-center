# Goal: Security Hardening and .NET 10 LTS Migration

Status: active
Owner: Human Partner and AI Agent
Risk: T3
Updated: 2026-08-02
Proposal: `PROPOSALS/2026-08-02--security-hardening-and-dotnet10.md`
Review Boundary: merge from `security/c011-dotnet10-and-hardening` into `main`

## Outcome

The actively deployed tmux-mobile service runs on .NET 10 LTS and satisfies the
approved security-review remediations 2-10 without changing the temporary access
key, product APIs, mobile workflow, exact Tailscale-IP host bind, or underlying
tmux workloads.

## Non-Goals

- Do not rotate or change the owner's temporary access key.
- Do not add identity providers, users, destructive actions, arbitrary commands,
  remote hosts, public ingress, or unrelated product features.
- Do not merge or push without separate owner approval.

## Acceptance Criteria

- [x] AC1 — All projects and production images use .NET 10; clean restore,
  build, tests, publish, container build/start, and dependency audits pass.
  - Evidence: .NET SDK 10.0.302 canonical verification passes; Release publish
    passes; `tmux-mobile:c011-net10` builds and reports ASP.NET/Core runtime
    10.0.10; NuGet and npm audits report no known vulnerabilities.
- [x] AC2 — Production cannot activate disabled or development authentication.
  - Evidence: validator and full-host startup regression reject Disabled plus
    the legacy production override; Development still requires both the
    Development environment and explicit local bypass.
- [ ] AC3 — PTY disconnect, timeout, failure, and shutdown reap the entire attach
  client process group without terminating the isolated tmux session.
  - Evidence: pending
- [ ] AC4 — Audits cover successful and allowed failed interactions, remain
  content-free and permission-protected, and audit failure cannot misreport an
  already-applied action as a retryable action failure.
  - Evidence: pending
- [ ] AC5 — Login, authenticated HTTP, terminal connection/input, and anonymous
  health limits use explicit tested partitions after correct middleware order.
  - Evidence: pending
- [x] AC6 — Production startup rejects unsafe origin, Host, proxy, HTTP, and
  authentication combinations with actionable errors.
  - Evidence: 25 server integration tests pass, including malformed/path/
    userinfo/query/HTTP origins, Host mismatch, proxy, unsafe-test
    acknowledgement, and disabled-auth startup negatives; all three Compose
    profiles render successfully with their intended posture.
- [ ] AC7 — Browser-facing HTTPS has HSTS, narrowed CSP, and authenticated API
  `no-store` headers while the xterm/PWA remains functional.
  - Evidence: pending
- [ ] AC8 — Anonymous liveness is inexpensive and process-executing readiness is
  local or explicitly authorized.
  - Evidence: pending
- [ ] AC9 — Docker remains bound only to the configured Tailscale IP; the live
  HTTPS Serve origin works and ordinary tailnet clients cannot use the direct
  backend HTTP port after the approved rollout.
  - Evidence: pending
- [ ] AC10 — Existing API, PWA, clipboard, scrollback, reconnect, tmux validation,
  and terminal lifecycle behavior remain compatible.
  - Evidence: pending
- [ ] AC11 — Documentation, rollback, roadmap, status, decisions, secret hygiene,
  canonical verification, live checks, and Change Review agree.
  - Evidence: pending

## Authority Envelope

### May Continue Without Asking

- Perform the approved repository-local T0-T2 implementation, tests, package and
  container downloads, .NET 10 development-SDK installation, documentation,
  commits, and review records.
- Rebuild and replace only the existing tmux-mobile deployment, preserve its
  ignored access key and persistent state, inspect and update its Tailscale Serve
  path, and perform bounded live validation without sending input to user panes.
- Apply the approved AC9 access restriction only after capturing the existing
  policy/configuration and proving a rollback path.

### Must Pause for Approval

- Changing or exposing the access key, interacting with user tmux pane contents,
  destructive/irreversible actions without tested recovery, unrelated host or
  tailnet policy, public exposure, scope expansion, compatibility breaks outside
  the proposal, merge, push, or publication.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. .NET 10 thin slice | completed | AC1 passes with no .NET 8 target/image | clean build/test/publish/image/audits |
| 2. Auth/config | completed | AC2 and AC6 pass | negative startup matrix |
| 3. Limits/health | in_progress | AC5 and AC8 pass | HTTP/WebSocket integration |
| 4. PTY lifecycle | pending | AC3 passes | fake and isolated real tmux |
| 5. Audit integrity | pending | AC4 passes | injected sink/action outcomes |
| 6. Browser/deploy | pending | AC7 and local AC9 pass | headers/PWA/Compose/proxy |
| 7. Deploy/review | pending | AC9-AC11 pass live | canonical/live/rollback/review |

## Progress

- 2026-08-02: owner approved proposal implementation through active deployment,
  excluding access-key replacement. C009 paused with only its final device
  acceptance open; C011 became the one active goal.
- 2026-08-02: installed SDK 10.0.302 into an isolated `/tmp` toolchain after
  system-wide installation required an interactive sudo password; migrated all
  projects, tests, global SDK selection, docs, and Noble container bases.
- 2026-08-02: canonical tests, Release publish, npm/NuGet audits, and production
  image build pass. The image contains ASP.NET/Core runtime 10.0.10 and no SDK;
  repository runtime targets contain no remaining .NET 8 target/image.
- 2026-08-02: removed the production-disabled-auth path, added exact HTTPS
  origin/Host/listener/proxy validation, and required the explicit
  `TAILNET_TEST_ONLY` acknowledgement for unsafe HTTP or weak-key profiles.
  Focused startup/configuration tests and all Compose renders pass.

## Evidence

- Proposal and 2026-08-02 security-review evidence establish scope.
- Official .NET support policy identifies .NET 10 as the latest LTS through
  2028-11-14.

## Discoveries

- Host currently has only .NET SDK 8.0.129; unit 1 must install or otherwise
  provide the .NET 10 SDK before canonical host verification can pass.
- Existing live container is healthy on the exact Tailscale-IP mapping before
  C011 changes.
- .NET 10 MCR publishes supported Noble SDK/runtime images but no Bookworm-slim
  tags; C011 uses the explicit Noble variants and installs tmux from Noble.

## Decisions

- Keep runtime migration behavior-neutral and finish it before security behavior
  changes so regressions remain attributable.
- Preserve C009 evidence by pausing rather than falsely completing its remaining
  physical-device acceptance criterion.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- Reorder and partition HTTP rate limiting, add bounded terminal input
  throughput, and restrict process-executing readiness for AC5 and AC8.

## Pause Conditions

- Pause on an unavailable supported .NET 10 toolchain, an unresolved runtime/API
  compatibility break, risk to ordinary tmux sessions, secret exposure, or an
  action beyond the Authority Envelope.

## Outcomes

- Pending.
