# Goal: Temporary HTTP Test Password

Status: completed
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-07-31
Proposal: `PROPOSALS/2026-07-31--temporary-test-password.md`
Review Boundary: merge from `feat/c005-temporary-test-password` into
`feat/c004-tailnet-http-test`

## Outcome

The owner can sign into the current exact-Tailscale-IP HTTP test instance with
`[redacted test key]`, while all normal deployments continue enforcing strong API keys.

## Non-Goals

- Do not disable authentication, broaden the listener, or change HTTPS defaults.

## Acceptance Criteria

- [x] AC1 — Short API keys remain rejected by default.
  - Evidence: focused validator test and canonical verification pass.
- [x] AC2 — The override requires API-key mode and guarded HTTP mode together.
  - Evidence: focused negative/positive validator cases pass.
- [x] AC3 — Live login with `[redacted test key]` succeeds and unauthenticated inventory is
  rejected.
  - Evidence: live HTTP statuses are login 204, authenticated inventory 200,
    and anonymous inventory 401.
- [x] AC4 — The listener remains only `100.85.13.102:8780`.
  - Evidence: Compose reports the exact mapping and `ss -ltn` reports only
    `100.85.13.102:8780`.
- [x] AC5 — Canonical verification and Change Review pass.
  - Evidence: 44 tests passed, one opt-in PTY test skipped, TypeScript and
    Compose checks passed; C005 Review Record is ready with explicit follow-up.

## Authority Envelope

### May Continue Without Asking

- Implement and test the approved fail-closed override.
- Replace the approved C005 test container on `100.85.13.102:8780`.

### Must Pause for Approval

- Any bind change, disabled authentication, public exposure, firewall/tailnet
  policy change, merge, push, TLS issuance, or other security exception.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Validation | completed | Override is fail-closed | focused tests |
| 2. Live thin slice | completed | `[redacted test key]` login succeeds | curl/health/listener |
| 3. Review | completed | Full gate and review pass | canonical verification |

## Progress

- 2026-07-31: owner explicitly approved `[redacted test key]` for the current test
  deployment.
- 2026-07-31: focused configuration tests passed 5/5.
- 2026-07-31: rebuilt the exact-IP container; it became healthy and emitted
  both unsafe-test warnings.
- 2026-07-31: live authentication and listener checks passed.
- 2026-07-31: canonical verification passed 44 tests with one opt-in PTY test
  skipped; TypeScript and Compose checks passed.
- 2026-07-31: implementation committed as `2bbeac0`; scope/security review found
  no blocking or high findings.

## Evidence

- AC1/AC2: `dotnet test ... --filter
  FullyQualifiedName~ConfigurationValidationTests` — 5 passed.
- AC3: live `curl` statuses — 401 anonymous, 204 login, 200 authenticated.
- AC4: `docker compose ... ps` and `ss -ltn` — healthy and exact-IP only.
- AC5: `./scripts/verify.sh` — exit 0; 44 passed, one skipped; Review Record is
  ready.

## Discoveries

- The existing validator intentionally rejects keys shorter than 24 characters.

## Decisions

- Require a separate conspicuous switch in addition to insecure HTTP mode.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- No execution action remains; owner may use the live URL with `[redacted test key]`.

## Pause Conditions

- Pause if the listener is not exact-IP, authentication cannot remain enabled,
  or the override affects the production Compose definition.

## Outcomes

- Completed with live evidence and an explicit requirement to remove the weak
  key override when moving to HTTPS.
