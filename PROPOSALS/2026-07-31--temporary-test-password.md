# Proposal: Temporary HTTP Test Password

Date: 2026-07-31
Owner: Human Partner and AI Agent
Risk Class: T2
Related Context: owner explicitly requested the access key `[redacted test key]`
Roadmap Item: C005
Planned Branch: `feat/c005-temporary-test-password`
Expected Commit Count: 2

## Objective

Use `[redacted test key]` for the current tailnet-only HTTP test instance without weakening
the normal API-key minimum or the production HTTPS deployment.

## Scope

In scope:

- A second explicit test-only switch that permits a short key only with guarded
  insecure-HTTP API-key mode.
- Updating the ignored local environment, rebuilding the current test
  container, and exercising live authentication.

Out of scope:

- Disabling authentication, changing the exact Tailscale-IP bind, committing
  the local key, or weakening the HTTPS configuration.

## Acceptance Criteria

- [x] Short keys remain rejected by default.
- [x] The override is rejected unless API-key mode and insecure HTTP are both
  explicit.
- [x] `[redacted test key]` authenticates against the live instance while unauthenticated
  inventory remains rejected.
- [x] The listener remains only `100.85.13.102:8780`.
- [x] Canonical verification and Change Review pass.

## Verification Plan

```bash
dotnet test tests/TmuxMobile.Server.IntegrationTests
./scripts/verify.sh
docker compose -f compose.http-test.yaml --env-file deploy/docker/.env config --quiet
```

Live evidence includes HTTP status codes, container health, and listener
address. Inventory content is not printed.

## Change Review Plan

- Review Boundary: merge into `feat/c004-tailnet-http-test`
- Planned Review Record: `REVIEWS/2026-07-31--temporary-test-password.md`

## Decomposition Plan

1. Add fail-closed validation and tests.
2. Recreate and exercise the live test deployment.
3. Run canonical verification and record the review.

Thin slice: `[redacted test key]` establishes an authenticated cookie on the exact-IP
test instance.

## Rollback Plan

Restore a random key in the ignored environment, disable the short-key switch,
and recreate the container. Revert C005 to remove the capability.

## Risks and Mitigations

- A guessable key reduces application-layer protection.
  Mitigation: user-approved temporary use, Tailscale-only exact bind, retained
  authentication/rate limiting, conspicuous switches and warnings.
- The override could leak into production.
  Mitigation: it is absent from `compose.yaml`, requires insecure HTTP, and is
  covered by negative validation tests.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-07-31 America/Chicago
