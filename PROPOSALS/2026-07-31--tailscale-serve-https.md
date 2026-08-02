# Proposal: Tailscale Serve HTTPS Cutover

Date: 2026-07-31
Owner: Human Partner and AI Agent
Risk Class: T2
Related Context: owner enabled Tailscale Serve HTTPS on port 8443
Roadmap Item: C006
Planned Branch: `feat/c006-tailscale-serve-https`
Expected Commit Count: 2

## Objective

Use `https://ubuntu-box-1.monster-ionian.ts.net:8443/` as the browser-facing
site while Tailscale Serve proxies to the exact-IP Docker backend, retaining
`[redacted test key]` temporarily.

## Scope

In scope:

- Exact Serve hostname/origin allowlists and a dedicated Compose profile.
- Secure cookies at the HTTPS browser boundary.
- An explicit eight-character test-key exception and live deployment cutover.

Out of scope:

- Changing the existing Tailscale Serve rule, using Funnel/public ingress,
  changing tailnet policy, or keeping the weak key as a final configuration.

## Acceptance Criteria

- [x] HTTPS root and health succeed through Tailscale Serve.
- [x] Login with `[redacted test key]` establishes Secure cookies; anonymous inventory is
  rejected and authenticated inventory succeeds.
- [x] Inventory WebSocket connects over the HTTPS origin.
- [x] Docker remains bound only to `100.85.13.102:8780`.
- [x] The production Compose profile retains the normal 24-character minimum.
- [x] Canonical verification and Change Review pass.

## Verification Plan

```bash
./scripts/verify.sh
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env config --quiet
curl --resolve ubuntu-box-1.monster-ionian.ts.net:8443:100.85.13.102 \
  https://ubuntu-box-1.monster-ionian.ts.net:8443/health/live
```

## Change Review Plan

- Review Boundary: merge into `feat/c005-temporary-test-password`
- Planned Review Record: `REVIEWS/2026-07-31--tailscale-serve-https.md`

## Decomposition Plan

1. Add secure Serve profile and bounded weak-key validation.
2. Build, replace the HTTP test container, and exercise the HTTPS thin slice.
3. Run canonical verification and record the review.

Thin slice: the HTTPS URL accepts `[redacted test key]` and loads authenticated inventory.

## Rollback Plan

Stop the Serve-profile container and restart `compose.http-test.yaml` with the
same ignored environment and persistent state. The owner may disable the Serve
rule separately if desired.

## Risks and Mitigations

- Guessable key: retain tailnet-only access, rate limiting, explicit warning,
  and a bounded eight-character minimum; rotate after validation.
- Proxy host/origin mismatch: fail closed and verify through the real Serve
  endpoint before handoff.
- Cutover interruption: build first, then replace the port holder and verify
  health immediately; rollback uses the prior Compose file.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-07-31 America/Chicago
