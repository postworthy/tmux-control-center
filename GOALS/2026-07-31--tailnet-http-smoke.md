# Goal: Authenticated Tailnet HTTP Smoke

Status: completed
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-07-31
Proposal: `PROPOSALS/2026-07-31--tailnet-http-smoke.md`
Review Boundary: merge from `feat/c004-tailnet-http-test` into
`chore/c001-adopt-tempo`

## Outcome

The owner can navigate to an API-key-authenticated HTTP instance published only
on this machine's Tailscale IP and inspect the real local tmux inventory.

## Non-Goals

- Do not make HTTP the production default or disable application authentication.
- Do not expose the service on loopback, LAN, or wildcard host addresses.

## Acceptance Criteria

- [x] AC1 — Secure production cookie behavior remains the default.
  - Evidence: default remains `__Host-TmuxMobile` with
    `CookieSecurePolicy.Always`; opt-in uses a separately named cookie.
- [x] AC2 — Explicit HTTP test mode authenticates with non-Secure Strict cookies.
  - Evidence: live response flags are HttpOnly=yes, SameSiteStrict=yes,
    Secure=no; disabled/Development authentication is rejected.
- [x] AC3 — Docker publishes only `100.85.13.102:8780`.
  - Evidence: rendered Compose and `ss -ltn` both show the exact address only.
- [x] AC4 — Live health, login, authenticated inventory, readiness, and the
  anonymous login shell return expected results.
  - Evidence: root/JS/CSS/manifest/service-worker all 200 with non-empty asset
    bodies; live/ready 200, unauthenticated inventory 401, login 204,
    authenticated inventory 200; headless Chrome rendered the login card,
    sign-in text, and access-key input.
- [x] AC5 — Canonical verification and Change Review pass.
  - Evidence: 42 tests passed, one opt-in test skipped, and the corrected C004
    Review Record is ready with the explicit TLS follow-up.

## Authority Envelope

### May Continue Without Asking

- Implement and test the approved guarded HTTP mode.
- Create ignored local configuration/state and run/replace the C004 test
  container on `100.85.13.102:8780`.

### Must Pause for Approval

- Any non-Tailscale bind, disabled authentication, public exposure, firewall or
  tailnet policy change, destructive action, merge, push, or TLS issuance.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Guarded cookies | completed | Defaults unchanged; opt-in tested | focused and canonical tests |
| 2. Live smoke | completed | Authenticated inventory loads on exact IP | Compose/curl |
| 3. Corrective review | completed | Login shell and full gate pass | asset traversal and canonical verification |

## Progress

- 2026-07-31: owner explicitly requested temporary HTTP operation.
- 2026-07-31: discovered tailnet HTTPS certificate support is not enabled.
- 2026-07-31: port 8080 was already held by an unrelated wildcard listener;
  selected unused port 8780 without altering the existing service.
- 2026-07-31: first authenticated inventory request exposed tmux 3.3a stripping
  tab delimiters; changed the machine format to a printable fail-closed
  delimiter and the live retry passed.
- 2026-07-31: container reached healthy state and all HTTP/API checks passed.
- 2026-07-31: implementation committed as `2cef73c`; final canonical
  verification and Change Review passed.
- 2026-07-31: owner reported an empty white page. RCA proved anonymous static
  JS/CSS requests returned 401 because authorization ran before static files.
- 2026-07-31: `f27414a` moved static shell serving before authorization, added
  linked-asset traversal coverage, and the exact live reproduction passed.

## Evidence

- Host Tailscale IP: `100.85.13.102`.
- Host owner: UID/GID `1000:1000`.
- Host tmux: 3.4 with socket `/tmp/tmux-1000/default`.
- Live listener: `100.85.13.102:8780`, with no loopback/LAN/wildcard bind.
- Live API: root/live/ready 200; unauthenticated 401; login 204; authenticated
  inventory 200.
- Canonical verification: 41 tests passed, one opt-in test skipped, TypeScript
  and Compose checks passed.

## Discoveries

- Existing `Secure` `__Host-` cookies cannot establish login over HTTP.
- Authentication must remain enabled; Development bypass is not acceptable.
- tmux 3.3a removes tab controls in format strings, while tmux 3.4 preserves
  them; a printable delimiter works with the container client and host server.

## Decisions

- Add a separately named test cookie and explicit warning rather than weakening
  the HTTPS default.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: root HTML returned 200 while anonymous JS/CSS returned 401,
  producing a blank browser page.

## Next Action

- No corrective action remains; ask the owner to reload the URL and confirm the
  visible login screen.

## Pause Conditions

- Pause if the host bind is broader than the Tailscale IP, authentication cannot
  remain enabled, or the real tmux socket requires elevated privileges.

## Outcomes

- Completed after RCA-backed correction and live dependency verification.
