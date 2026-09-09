# Change Review: Expired authentication recovery

Date: 2026-09-09
Risk: T1
Source: `fix/expired-auth-reprompt`
Target / boundary: normal merge into `main` and push to `origin/main`, explicitly authorized by the user on 2026-09-09.
Proposal: `PROPOSALS/2026-09-09--expired-auth-reprompt.md`
Decision: ready for authorized integration; no blocking findings.
Reviewer: Codex, self-review

## Scope and acceptance

AC1: Both API wrappers signal protected-request 401 to a shared in-memory store.
Desktop and PWA subscribe to that store. Inventory messages and successful GETs
cannot dismiss it. PWA terminal rendering yields to the prompt. Inventory sockets
stop while authentication is required and restart after login. Stale mobile socket
callbacks cannot incorrectly mark a replacement connection offline.

AC2: Successful login clears cached CSRF and the auth latch. Session-name state,
selected server URL, and parent workspace state are retained. No mutation is
replayed automatically. Tests cover both wrappers, actual mounted desktop/PWA
forms, rejected and successful login, retained draft name, and manual creation.

AC3: Spec and desktop guide document recovery and temporary terminal detachment.
Verification below passed. No server auth policy, permission, storage, protocol,
password persistence, or migration changes. No unrelated files included.

## Verification

- `npm --prefix src/TmuxMobile.Web run typecheck`: pass.
- `npm --prefix src/TmuxMobile.Web run test:unit`: 16 suites pass, including the
  new desktop/mobile expiry, fresh-CSRF, no-replay and non-auth-error regression.
- `npm --prefix src/TmuxMobile.Web run build`: both frontend builds pass; standard
  bundle-size advisory. Generated tracked web assets restored afterward.
- `PATH=/tmp/expired-auth-dotnet:$PATH ./scripts/verify.sh`: pass after final
  socket guard; 166 .NET tests, 16 frontend suites, shell recovery/setup/native
  delivery checks and Compose checks. Initial plain invocation could not resolve
  the pinned 10.0.300 SDK because system dotnet is 8.0.130. Installed the pinned
  SDK in `/tmp` without changing repository SDK requirements or system tooling.
- `PYTHONPATH=/tmp/curiosity-playwright python -u /tmp/expired-auth-browser.py`:
  Chromium simulation passes on both Vite desktop and PWA pages. Start with
  authenticated empty inventory, expire protected API responses, submit create,
  observe password prompt, reject wrong key, accept correct key, observe retained
  server/name and manually create exactly once. Browser harness was corrected
  to select desktop's create button by its form; the PWA run exposed and then
  verified the stale socket callback correction.
- `git diff --check`: pass.

Limits: browser tests simulate HTTP expiry; they do not wait eight hours or claim
physical Photino/iPhone acceptance. Deployment evidence is recorded below.

## Rollback and git conformance

The source branch is a feature branch. Commit the scoped change there and use a normal merge commit.
Revert only the scoped frontend sources, test, spec/guide, proposal, goal and this
review. There is no state migration. The pre-existing untracked
`RCA/2026-09-08--remote-codex-scroll-binding.md` is untouched.

## Approval

User authorized option A or B; implemented option A. Local implementation is
review-ready. The user subsequently authorized deployment and then explicitly
authorized commit, merge to main, and push to the configured upstream.

## Authorized deployment follow-up

After local review, the user requested `deploy`. On 2026-09-09 built and deployed
`tmux-mobile:expired-auth-20260909` through the existing Compose profile and
persisted that image tag in the ignored deployment environment. Isolated tmux
3.4 compatibility probe passed. Container is healthy; HTTPS `/health/live` is
200, unauthenticated `/api/sessions` is 401, and direct backend `/` is 426.
Both live root and `/desktop/` bundles contain the new expiry handling and prompt.
PWA release cache: `tmux-mobile-shell-e437634c377eb4e5`. Existing authenticated
inventory and terminal clients reconnected in bounded post-start logs.

Origin: `https://ubuntu-box-1.monster-ionian.ts.net:8443`.
Preserved Serve routes, state mounts and credentials. No git merge or push.
Rollback image retained: `tmux-mobile:desktop-system-clipboard-20260908`.
Rollback: run Compose up with `TMUX_MOBILE_IMAGE_TAG=desktop-system-clipboard-20260908`
and the existing profile/env; restore the ignored env's image tag accordingly.
No second physical-device check was performed. Existing desktop pages need a
reload/reopen; the PWA must apply its waiting update to load the new JavaScript.

## Integration scope

C025 includes the frontend sources, authentication regression test, desktop guide,
spec, proposal, goal, decision record, roadmap entry and this review. The final
canonical verification remains current: no application changes since that pass.
Upstream was fetched before integration; no conflicting upstream work found.
