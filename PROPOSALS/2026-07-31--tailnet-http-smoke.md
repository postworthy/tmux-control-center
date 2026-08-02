# Proposal: Authenticated Tailnet HTTP Smoke Deployment

Date: 2026-07-31
Owner: Human Partner and AI Agent
Risk Class: T2
Related Context: owner requested HTTP until the site is working
Roadmap Item: C004
Planned Branch: `feat/c004-tailnet-http-test`
Expected Commit Count: 2

## Objective

Run a temporary API-key-authenticated HTTP deployment on this host's exact
Tailscale IP so the owner can exercise the site before enabling tailnet HTTPS.

## Scope

In scope:

- Explicit opt-in insecure-cookie support limited to API-key mode.
- A separate HTTP smoke Compose definition bound to required `TAILSCALE_IP`.
- An ignored host `.env`, protected state, live deployment, and smoke tests.

Out of scope:

- Disabling authentication, binding LAN/loopback/wildcard host addresses, or
  changing the production HTTPS Compose definition.
- Tailscale certificate enablement, public ingress, or long-term HTTP use.

## Acceptance Criteria

- [x] Production remains secure-cookie HTTPS by default.
- [x] HTTP mode requires both API-key authentication and an explicit unsafe
  switch, and logs a warning.
- [x] The host listener is only `100.85.13.102`.
- [x] Login, authenticated inventory, liveness, and readiness are exercised.
- [x] The owner receives the working URL and local key location.

## Verification Plan

```bash
./scripts/verify.sh
docker compose -f compose.http-test.yaml --env-file deploy/docker/.env config
curl http://100.85.13.102:8780/health/live
```

Pass means automated tests pass, the listener is address-scoped, unauthenticated
API access is rejected, login establishes a cookie, and authenticated inventory
returns successfully.

## Change Review Plan

- Review Boundary: merge into `chore/c001-adopt-tempo`
- Planned Review Record: `REVIEWS/2026-07-31--tailnet-http-smoke.md`
- Reviewer/approver expectation: evidence-backed review; HTTPS migration remains
  required before treating the deployment as final.

## Decomposition Plan

1. Add guarded HTTP cookie mode and tests — Verify by focused and canonical
   tests — Exit: opt-in works without changing defaults — Risk: T2.
2. Add Compose smoke definition and ignored environment — Verify by rendered
   address and live HTTP/API checks — Exit: owner can navigate to the service —
   Risk: T2 — Dependency: unit 1.
3. Record evidence and review — Exit: clear ready/rollback decision — Risk: T0.

Thin slice milestone:

- An authenticated session inventory loads at the Tailscale-IP URL.

## Rollback Plan

1. Run `docker compose -f compose.http-test.yaml --env-file deploy/docker/.env down`.
2. Remove the ignored test environment/state if desired.
3. Revert C004 and run `./scripts/verify.sh`.

## Risks and Mitigations

- HTTP is not a browser-secure transport.
  Mitigation: exact Tailscale-only bind, API-key authentication, Strict cookies,
  explicit unsafe switch, and temporary-use warning.
- The tmux client may be incompatible with the host server.
  Mitigation: exercise readiness and inventory before handing off the URL.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-07-31 America/Chicago
