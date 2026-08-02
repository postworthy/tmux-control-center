# RCA: Blank Anonymous App Shell

Date: 2026-07-31
Severity: High
Related Proposal: `PROPOSALS/2026-07-31--tailnet-http-smoke.md`
Related Commits: `2cef73c`, `7e4d8f5`

## Symptom

- The owner opened `http://100.85.13.102:8780/` and saw an empty white page
  instead of the login UI.
- The prior handoff claimed the URL was usable because the root document and API
  smoke checks passed.

## Reproduction

1. Request `/` without authentication: HTTP 200 with an HTML shell.
2. Read the linked module and stylesheet paths from that HTML.
3. Request `/assets/index-CbeY5vct.js` without authentication: HTTP 401.
4. Request `/assets/index-Dl2A9W7u.css` without authentication: HTTP 401.
5. The browser therefore receives an empty `<div id="root"></div>` with neither
   React nor CSS, producing a white page.

## Evidence

- The built image contains both requested assets at the expected paths.
- Direct live requests to both assets return 401 with empty bodies.
- `Program.cs` places `UseAuthentication` and `UseAuthorization` before
  `UseStaticFiles`, while the authorization fallback policy requires `Read`.
- `/` was allowed through the explicit anonymous SPA fallback, but its static
  dependencies had no anonymous endpoint metadata.

## Root Cause

- Implementation layer: middleware ordering caused the fallback authorization
  policy to protect static application-shell files required to display the
  unauthenticated login screen.
- Verification layer: the live check asserted only root status 200 and API
  behavior. It never followed the HTML's script and stylesheet dependencies or
  asserted visible client rendering.
- The earliest divergence was treating “HTML shell served” as “application
  rendered.”

## Corrective Action

- Serve static application-shell files before authentication/authorization
  middleware. API and WebSocket endpoints retain their explicit authorization.
- Add an integration test that requests `/` anonymously, extracts every local
  script/stylesheet dependency, and requires HTTP 200 for each.
- Rebuild the live container and repeat the exact asset requests plus the
  original root/API smoke checks.

## Preventive Controls

- Test: anonymous app-shell dependency traversal in server integration tests.
- Review: a frontend smoke handoff must verify linked assets, not only the HTML
  status code.
- Process: physical/browser observations override HTTP-shell health claims.

## Resolution Evidence

- Corrective commit: `f27414a`.
- Focused anonymous asset traversal test: pass.
- Live root, JavaScript, CSS, manifest, and service-worker requests: HTTP 200
  with non-empty bodies.
- Live API boundary remains intact: unauthenticated inventory 401, login 204,
  authenticated inventory 200.
- Headless Chrome with a three-second render budget produced a React DOM
  containing `class="login-card"`, the sign-in text, and `id="api-key"`.
- Canonical verification: pass with 42 tests and one opt-in PTY test skipped.
