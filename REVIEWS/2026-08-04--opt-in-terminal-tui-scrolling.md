# Review Record: Opt-in Terminal TUI Scrolling

Date: 2026-08-04
Review Boundary: merge from `feat/c012-opt-in-tui-scroll` into `main`
Merge Method: `git merge --no-ff feat/c012-opt-in-tui-scroll`
Risk Class: T1
Related Proposal: `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`

## Decision

Pending physical-device acceptance. Local implementation, negotiated xterm
encoding, isolated protocol evidence, canonical verification, and diff review
pass. No merge, push, publication, or deployment is authorized by this record.

## Scope and Compatibility

- [x] Default vertical swipes retain the existing typed tmux-history message.
- [x] Application scrolling requires an explicit accessible pressed-state
  control and is never persisted.
- [x] Disconnect, terminal entry, and terminal exit reset the mode to off.
- [x] Enabled gestures emit only one to three negotiated wheel events; no
  app-specific command, raw caller payload, backend route, or schema was added.
- [x] Existing Older/Latest, typing, modifiers, paste, resize, reconnect, and
  cleanup paths remain present.
- [ ] Physical iPhone behavior is accepted for normal history, Claude Code, and
  one additional mouse-aware TUI such as mitmproxy.

## Evidence

- Frontend `test:unit`: pass, including unchanged default JSON and bounded signed
  application-wheel routing.
- Frontend `typecheck`: pass.
- Temporary loopback-only headless-Chrome probe: installed xterm 6 accepts the
  synthetic line-mode wheel event and emits SGR wheel-up bytes
  `27,91,60,54,52,...,77`; the temporary page and server were removed.
- `docker build --target server-build --tag tmux-mobile:c012-test .`: pass on
  pinned .NET 10 and production Vite build.
- Disposable .NET 10 container with tmux: four LinuxIntegration tests pass.
  The new test observes `alternate_on=1`, `mouse_any_flag=1`, and SGR mode before
  a wheel-up report crosses the attached tmux client into an isolated raw-mode
  foreground program; no owner socket or pane is addressed.
- Canonical `./scripts/verify.sh`: pass with 24 Core, 12 Infrastructure (four
  opt-in Linux tests skipped in this run), 33 Server integration, frontend
  typecheck and both unit suites, and safe Compose validation.
- Owner-approved tailnet test deployment: previous image `sha256:d2b52a...` is
  tagged `tmux-mobile:pre-c012-rollback`; deployed image `sha256:00eb6c...` is
  healthy. HTTPS liveness/root and container readiness return 200, the served
  terminal bundle contains `App Scroll`, tmux 3.4 enumerates current sessions,
  direct backend root remains 426, and startup logs contain no new errors.

## Risk and Rollback

- Enabled application scrolling is terminal input and may act contextually in a
  foreground TUI. Default-off, explicit activation, visible state, bounded fixed
  wheel semantics, modifier isolation, and reset-on-loss mitigate this risk.
- Synthetic WheelEvent acceptance still requires physical iOS Safari evidence.
- Rollback is a revert of the C012 commits followed by frontend tests and
  `./scripts/verify.sh`; the default path already matches the pre-C012 contract.

## Findings

- Blocking: physical-iPhone acceptance remains.
- Non-blocking: none recorded.
