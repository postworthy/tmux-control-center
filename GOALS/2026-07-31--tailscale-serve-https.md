# Goal: Tailscale Serve HTTPS Cutover

Status: completed
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-07-31
Proposal: `PROPOSALS/2026-07-31--tailscale-serve-https.md`
Review Boundary: merge from `feat/c006-tailscale-serve-https` into
`feat/c005-temporary-test-password`

## Outcome

The owner can use the Tailscale Serve HTTPS URL with `[redacted test key]`, Secure cookies,
and live tmux access while the Docker backend remains exact-IP only.

## Non-Goals

- Do not alter Serve/tailnet policy, expose Funnel, broaden the bind, or make the
  weak credential permanent.

## Acceptance Criteria

- [x] AC1 — HTTPS root and health succeed through the exact Serve hostname.
  - Evidence: live HTTPS root and liveness returned 200 through Serve.
- [x] AC2 — `[redacted test key]` login uses Secure cookies and preserves API authorization.
  - Evidence: login 204; cookie flags Secure/HttpOnly/SameSite=Strict; anonymous
    inventory 401 and authenticated inventory 200.
- [x] AC3 — The inventory WebSocket connects over the HTTPS origin.
  - Evidence: authenticated `wss://` handshake through Serve completed with
    origin validation and no session payload logged.
- [x] AC4 — Docker remains bound only to `100.85.13.102:8780`.
  - Evidence: Compose `ps` and `ss -ltn` report the exact address only.
- [x] AC5 — Normal production configuration retains strong-key validation.
  - Evidence: focused negative test passes and `compose.yaml` does not enable
    the weak-key option.
- [x] AC6 — Canonical verification and Change Review pass.
  - Evidence: 44 tests passed, one opt-in PTY test skipped, TypeScript and
    Compose checks passed; C006 Review Record is ready with explicit follow-ups.

## Authority Envelope

### May Continue Without Asking

- Implement and test the approved Serve profile and guarded weak-key option.
- Build the replacement image, stop the prior C005 test container, and start the
  C006 container on the same exact-IP port.

### Must Pause for Approval

- Any Serve/tailnet policy command, Funnel/public exposure, broader bind,
  disabled authentication, destructive state removal, merge, or push.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Secure profile | completed | Config fails closed and cookies are Secure | tests/Compose |
| 2. HTTPS thin slice | completed | Live login and inventory work | curl/WebSocket/listener |
| 3. Review | completed | Full gate and review pass | canonical verification |

## Progress

- 2026-07-31: owner supplied and approved the active Tailscale Serve HTTPS URL
  and requested retaining `[redacted test key]` temporarily.
- 2026-07-31: pre-change HTTPS health reached Kestrel but failed 400 because the
  public hostname was not yet allowed.
- 2026-07-31: focused validation passed 5/5 after correcting two stale test
  expectations during a bounded two-attempt retry.
- 2026-07-31: built first, replaced only the prior HTTP-test container, and the
  Serve-profile container became healthy.
- 2026-07-31: HTTPS root/health, Secure-cookie login, API authorization, WSS,
  and exact listener checks passed.
- 2026-07-31: canonical verification passed 44 tests with one opt-in PTY test
  skipped; TypeScript and production Compose checks passed.
- 2026-07-31: implementation committed as `1b9a927`; the exact committed image
  passed the final health, HTTPS login, API, WSS, cookie, and listener checks.
- 2026-07-31: scope/security review found no blocking or high findings.

## Evidence

- AC1: HTTPS root=200 and live=200 through the exact hostname/SNI.
- AC2: login=204, Secure=yes, HttpOnly=yes, SameSiteStrict=yes; anonymous=401,
  authenticated=200.
- AC3: authenticated WSS command exited 0; payload output suppressed.
- AC4: healthy container and `100.85.13.102:8780` only.
- AC5: focused configuration suite 5 passed.
- AC6: canonical verification exit 0; 44 passed, one skipped; Review Record is
  ready with weak-key rotation and physical-iPhone validation follow-ups.

## Discoveries

- Tailscale Serve preserves the public Host header to this backend.
- The host cannot resolve its own MagicDNS name; live verification can preserve
  TLS SNI using `curl --resolve` with the exact Tailscale IP.

## Decisions

- Restore Secure cookies instead of carrying the non-Secure HTTP-test cookie
  through the HTTPS proxy.
- Do not trust proxy identity or forwarded headers; application API-key auth
  remains authoritative.

## Retry State

- Current attempt: 2
- Maximum attempts per unchanged failure: 2
- Last failure: resolved test-expectation mismatch; implementation behavior was
  unchanged.

## Next Action

- No execution action remains; owner may use the HTTPS URL with `[redacted test key]`.

## Pause Conditions

- Pause if Serve cannot proxy WebSockets, the listener broadens, authentication
  cannot remain enabled, or the public endpoint becomes reachable outside the
  tailnet.

## Outcomes

- Completed with live HTTPS/WSS evidence. The weak key remains a deliberate
  temporary exception and must be rotated after validation.
