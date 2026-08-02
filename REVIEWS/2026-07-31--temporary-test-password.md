# Review Record: Temporary HTTP Test Password

Date: 2026-07-31
Review Boundary: merge from `feat/c005-temporary-test-password` into
`feat/c004-tailnet-http-test`
Merge Method: `git merge --no-ff feat/c005-temporary-test-password`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-07-31--temporary-test-password.md`

## Decision

Ready with explicit follow-up. The current exact-Tailscale-IP HTTP test instance
accepts `[redacted test key]`, while default validation and the HTTPS Compose deployment
continue requiring a key of at least 24 characters. Remove this override and
replace the weak key when HTTPS is enabled.

## Commits in Scope

- `2bbeac0` feat(auth): allow explicit short HTTP test key

The review-record commit is documentation-only evidence created afterward.

## Scope and Git

- [x] Work is on the approved feature branch.
- [x] The implementation stays within the explicitly approved test credential
  exception.
- [x] `compose.yaml` does not enable the short-key override.
- [x] The local `.env` remains ignored; `[redacted test key]` is absent from the tracked
  diff.
- [x] No listener, tailnet policy, authentication mode, or unrelated container
  was changed.

## Acceptance Evidence

- [x] A short key is rejected when the override is absent.
- [x] The override is rejected without guarded insecure HTTP.
- [x] Disabled authentication cannot use the override.
- [x] The guarded API-key/insecure-HTTP combination accepts the short test key.
- [x] Startup emits a distinct weak-key warning.
- [x] Anonymous inventory returns 401.
- [x] Login with `[redacted test key]` returns 204.
- [x] Authenticated inventory returns 200 without logging response contents.
- [x] The container is healthy and Docker publishes only
  `100.85.13.102:8780`.

## Verification

```bash
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj \
  --filter FullyQualifiedName~ConfigurationValidationTests --no-restore
```

- Pass: 5 tests.

```bash
./scripts/verify.sh
```

- Pass: 24 Core, 7 Infrastructure, and 13 Server tests; one opt-in isolated PTY
  test skipped; TypeScript and Compose checks passed.

```text
container=healthy listener=100.85.13.102:8780
anonymous_inventory=401 login=204 authenticated_inventory=200
```

## Findings

- Blocking/high: none.
- Medium: `[redacted test key]` is guessable and must remain limited to this owner-approved
  temporary tailnet-only HTTP test.
- Medium: HTTP lacks browser TLS; migration to the production HTTPS Compose
  configuration remains required.
- Low: the HTTP test Compose file always enables the short-key exception,
  deliberately making that file unsuitable for production.

## Compatibility and Rollback

- Existing configuration is backward compatible because the new option defaults
  to false.
- The production HTTPS Compose definition and secure-cookie behavior are
  unchanged.
- Rollback: restore a strong random value in the ignored `.env`, remove the
  short-key switch from the HTTP test Compose file, recreate the container, and
  run `./scripts/verify.sh`. Reverting C005 removes the capability entirely.

## Approvals

- Weak test key and live container replacement: repository owner, 2026-07-31.
- Reviewer: Codex, evidence-backed local review.
- Status: ready with explicit HTTPS/strong-key follow-up.
- Merge/push: not authorized or implied.

## Follow-Ups

- Use `[redacted test key]` only for the current tailnet test.
- Enable HTTPS, replace the key with a strong random secret, and remove the
  short-key test override before treating the deployment as final.
