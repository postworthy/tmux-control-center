# Review Record: Tailscale Serve HTTPS Cutover

Date: 2026-07-31
Review Boundary: merge from `feat/c006-tailscale-serve-https` into
`feat/c005-temporary-test-password`
Merge Method: `git merge --no-ff feat/c006-tailscale-serve-https`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-07-31--tailscale-serve-https.md`

## Decision

Ready with explicit follow-ups. The tailnet-only Serve URL now fronts the
exact-IP Docker backend with HTTPS/WSS and Secure cookies. `[redacted test key]` remains a
deliberate temporary weak-key exception; it must be replaced before this
deployment is treated as final.

## Commits in Scope

- `1b9a927` feat(deploy): add Tailscale Serve HTTPS profile

The review-record commit is documentation-only evidence created afterward.

## Scope and Git

- [x] Work is on the approved feature branch and matches C006 scope.
- [x] Existing Tailscale Serve and tailnet policy were inspected but not changed.
- [x] `compose.yaml` does not enable the weak-key exception.
- [x] The ignored `.env` and credential are absent from the tracked diff.
- [x] Only the prior C005 test container was replaced; no unrelated listener or
  container was modified.

## Acceptance Evidence

- [x] Pre-change HTTPS reproduced 400 Invalid Hostname.
- [x] Post-change HTTPS root and liveness return 200.
- [x] Login with `[redacted test key]` returns 204.
- [x] Authentication cookie is `__Host-TmuxMobile` with Secure, HttpOnly, and
  SameSite=Strict.
- [x] Anonymous inventory returns 401 and authenticated inventory returns 200.
- [x] Authenticated inventory WSS handshake succeeds through Serve with the
  exact HTTPS origin.
- [x] Container is healthy and publishes only `100.85.13.102:8780`.
- [x] `tailscale serve status` reports port 8443 as tailnet-only and proxying to
  `http://100.85.13.102:8780`.

## Verification

```bash
dotnet test tests/TmuxMobile.Server.IntegrationTests/TmuxMobile.Server.IntegrationTests.csproj \
  --filter FullyQualifiedName~ConfigurationValidationTests --no-restore
```

- Pass: 5 tests, including eight-character lower bound, wrong-mode rejection,
  and default 24-character enforcement.

```bash
./scripts/verify.sh
```

- Pass: 24 Core, 7 Infrastructure, and 13 Server tests; one opt-in isolated PTY
  test skipped; TypeScript and production Compose checks passed.

```text
https_root=200 live=200 login=204 anonymous=401 authenticated=200 wss=connected
cookie=__Host-TmuxMobile secure=yes httponly=yes samesite_strict=yes
container=healthy listener=100.85.13.102:8780
```

- Tmux inventory and WebSocket snapshot content were suppressed.

## Findings

- Blocking/high: none.
- Medium: `[redacted test key]` is guessable and remains suitable only for this
  owner-approved temporary tailnet validation.
- Medium: the backend port is reachable directly within the tailnet because the
  owner requires exact-Tailscale-IP binding. The application does not trust
  proxy identity/forwarded headers, Host/origin allowlists remain exact, and
  browser authentication uses Secure cookies.
- Low: this host does not resolve its own MagicDNS name. Verification used
  `curl --resolve` to retain the real hostname and TLS SNI while targeting the
  exact Tailscale IP.
- Low: physical iPhone installed-PWA and terminal interaction remain manual
  validation steps.

## Compatibility and Rollback

- HTTP/WebSocket API contracts are unchanged; frontend-relative WebSocket URLs
  automatically select WSS under HTTPS.
- The weak option was renamed from the unmerged C005 HTTP-specific name to the
  protocol-neutral test name; both checked-in test profiles were updated.
- Rollback: stop `compose.tailscale-serve.yaml`, restart
  `compose.http-test.yaml`, verify exact listener/health, and optionally disable
  the Serve rule separately. Persistent keys and audit state are preserved.

## Approvals

- Serve URL use, temporary `[redacted test key]`, and container cutover: repository owner,
  2026-07-31.
- Reviewer: Codex, evidence-backed local review.
- Status: ready with explicit weak-key rotation and iPhone follow-ups.
- Merge/push: not authorized or implied.

## Follow-Ups

- Validate installed-PWA login, session cards, WSS reconnect, and terminal input
  from the iPhone using the HTTPS URL.
- Replace `[redacted test key]` with a strong random key and remove
  `Authentication__UnsafeAllowWeakApiKeyForTest` after validation.
