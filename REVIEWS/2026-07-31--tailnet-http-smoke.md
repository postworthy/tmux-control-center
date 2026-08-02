# Review Record: Authenticated Tailnet HTTP Smoke

Date: 2026-07-31
Review Boundary: merge from `feat/c004-tailnet-http-test` into
`chore/c001-adopt-tempo`
Merge Method: `git merge --no-ff feat/c004-tailnet-http-test`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-07-31--tailnet-http-smoke.md`

## Decision

Ready with explicit follow-up after corrective review. The blank-page report was
reproduced and resolved under
`RCA/2026-07-31--blank-anonymous-app-shell.md`; anonymous app-shell dependencies
now load while API authorization remains enforced. HTTP must not replace the
HTTPS production configuration.

## Commits in Scope

- `2cef73c` feat(deploy): add authenticated tailnet HTTP smoke mode
- `f27414a` fix(server): serve anonymous app shell assets

The review-record commit is documentation-only evidence created afterward.

## Scope and Git

- [x] Work is on the approved feature branch.
- [x] Commit is conventional and contains Roadmap/Proposal trailers.
- [x] Production HTTPS Compose and secure-cookie defaults remain unchanged.
- [x] Ignored `.env`, access key, cookie keys, audit data, and runtime state are
  absent from the diff.
- [x] No unrelated listener or container was stopped or modified.

## Acceptance Evidence

- [x] `Authentication:UnsafeAllowInsecureHttp` is rejected unless API-key mode
  is active.
- [x] Default cookie remains `__Host-TmuxMobile` with Secure=Always.
- [x] Test cookie is separately named, HttpOnly, SameSite=Strict, and non-Secure.
- [x] Startup emits an explicit unsafe-test warning.
- [x] Docker and `ss` show only `100.85.13.102:8780`.
- [x] Root, liveness, and readiness return 200.
- [x] Every linked anonymous JS/CSS dependency returns 200 with a non-empty
  body.
- [x] Unauthenticated inventory returns 401.
- [x] Login returns 204 and authenticated real-tmux inventory returns 200.
- [x] Container health is `healthy`.

## Verification

```bash
./scripts/verify.sh
```

- Pass: 24 Core, 7 Infrastructure, and 11 Server tests; one opt-in isolated PTY
  test skipped; TypeScript and production Compose checks passed.

```text
http://100.85.13.102:8780/
root=200 js=200 css=200 manifest=200 service_worker=200
live=200 ready=200
unauthenticated=401 login=204 authenticated_inventory=200
```

- Live inventory response content was not printed or logged.
- Headless Chrome rendered `class="login-card"`, the private-control-service
  sign-in text, and the `api-key` input from the live URL.
- Image:
  `sha256:9c65e8198db91fd69271ed130d79d83250a4266ea5f6bf7e6ad246ffc82c5711`.

## Findings

- Blocking/high: none.
- Medium: HTTP lacks browser TLS and must remain a temporary tailnet-only test.
- Resolved High: anonymous fallback authorization blocked app-shell JS/CSS;
  `f27414a` reorders static middleware and adds dependency traversal coverage.
- Low: port 8080 was occupied by an unrelated wildcard listener; the deployment
  uses unused port 8780 without changing that listener.
- Low: tmux 3.3a strips tab format delimiters. C004 uses a printable,
  fail-closed delimiter and live inventory against host tmux 3.4 passes.

## Rollback

1. Run:
   `docker compose -f compose.http-test.yaml --env-file deploy/docker/.env down`.
2. Revert the C004 review and implementation commits.
3. Run `./scripts/verify.sh`.
4. The reviewed HTTPS deployment remains available unchanged.

## Approvals

- HTTP test and live start: repository owner, 2026-07-31.
- Reviewer: Codex, evidence-backed local review.
- Status: ready with explicit TLS follow-up.
- Merge/push: not authorized or implied.

## Follow-Ups

- Exercise session cards and terminal interaction from the iPhone.
- Enable Tailscale HTTPS, provision/renew the certificate, stop this HTTP mode,
  and deploy `compose.yaml`.
