# Review Record: Focus-Neutral Application Scroll

Date: 2026-08-05
Review Boundary: merge from `fix/c015-app-scroll-keyboard-focus` into `main`
Merge Method: `git merge --no-ff fix/c015-app-scroll-keyboard-focus`
Risk Class: T1
Related Proposal: `PROPOSALS/2026-08-05--focus-neutral-application-scroll.md`
Related RCA: `RCA/2026-08-05--application-scroll-opens-keyboard.md`

## Decision

Pending canonical/image/deployment checks and physical iPhone acceptance. No
merge, push, or publication is authorized by this record.

## Scope and Compatibility

- [x] Shared application-wheel dispatch contains no focus call.
- [x] App Scroll toggle contains no focus call.
- [x] Connection/reconnection, terminal keys, Ctrl/Alt modifiers, and paste
  completion retain their intentional focus calls.
- [x] No routing, wheel, velocity, coalescing, storage, backend, limiter, schema,
  tmux, authentication, or network behavior changed.
- [x] Canonical verification and production image pass.
- [ ] Deployed physical swipes and application-mode Older/Latest keep the iPhone
  keyboard closed and terminal connected.

## Evidence

- Installed xterm 6 source maps `Terminal.focus()` directly to
  `textarea.focus({ preventScroll: true })`.
- Complete TerminalView focus enumeration finds four retained caller categories:
  connection, actual shortcut keys, Ctrl/Alt modifiers, and paste completion.
  App Scroll dispatch and toggle contain none.
- Frontend unit suites and typecheck: pass.
- Canonical `./scripts/verify.sh`: pass with 24 Core, 12 Infrastructure (four
  isolated tests skipped), 33 Server integration, all three frontend suites,
  and Compose validation.
- Production image `sha256:2bc9c5...`: pass with only current application
  bundles, retained C013/C014 markers, and worker cache
  `tmux-mobile-shell-f3ef9d969ed07f10`.
- Owner-approved tailnet deployment: pass. C014 is preserved as
  `tmux-mobile:pre-c015-focus-rollback`; live image `sha256:2bc9c5...` is healthy
  on the unchanged exact-IP bind. HTTPS liveness/root, readiness, current asset
  identity, compatibility across 13 tmux sessions, and direct-backend 426 checks
  pass; no startup rate-limit warning or new error is present.

## Risk and Rollback

- The two removed statements have no role in wheel encoding or delivery; xterm
  processes dispatched wheel events without textarea focus.
- Rollback is a C015 revert or restoration of the preserved pre-C015 image; no
  data or configuration migration exists.

## Findings

- Blocking: physical acceptance.
- Non-blocking: none recorded.
