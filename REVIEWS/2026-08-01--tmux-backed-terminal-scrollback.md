# Review Record: tmux-Backed Terminal Scrollback Correction

Date: 2026-08-01
Review Boundary: merge from `fix/c009-tmux-backed-scrollback` into
`feat/c008-terminal-touch-scrollback`
Merge Method: `git merge --no-ff fix/c009-tmux-backed-scrollback`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-01--tmux-backed-terminal-scrollback.md`
Related RCAs: `RCA/2026-08-01--xterm-local-scrollback-noop.md`,
`RCA/2026-08-01--latest-banner-layout-miss.md`,
`RCA/2026-08-01--latest-pinned-layout-mismatch.md`,
`RCA/2026-08-01--history-controls-order-mismatch.md`

## Decision

Ready. The correction replaces ineffective xterm-local scrolling with typed,
bounded tmux copy-mode page actions and has frontend, WebSocket, exact-command,
real isolated-tmux, canonical, live-deployment, and successful physical-iPhone
evidence. The owner supplied the final required device acceptance on 2026-08-02.

## Commits in Scope

- `a468360` docs(rca): record terminal scrollback boundary failure
- `0774ab4` fix(terminal): navigate tmux-backed scrollback
- `c3f93be` fix(security): rate limit terminal history control
- `81d6d95` fix(terminal): keep latest history control visible
- `f757a10` docs(rca): record latest control layout miss
- `2faa017` fix(terminal): integrate latest into shortcut row
- `393439d` docs(rca): clarify latest shortcut placement
- `55c9f70` fix(terminal): restore latest beside older
- `fccf444` docs(rca): clarify history control priority
- `1383b83` fix(terminal): prioritize history controls

The eventual review-status update is documentation-only evidence afterward.

## Scope and Git

- [x] Work is on the approved C009 corrective branch.
- [x] Diff contains only the RCA/contracts, typed history protocol, fixed tmux
  control, tests, generated PWA assets, documentation, and review hardening.
- [x] No authentication, origin, listener, tailnet, history-depth, tmux config,
  arbitrary command, or shell-execution capability changed.
- [x] Local environment, secrets, logs, test sockets, and runtime state are
  absent from git.
- [x] Only the approved Tailscale Serve test container was rebuilt.

## Acceptance Evidence

- [x] One-finger axis lock remains thresholded; touch completion emits one
  bounded older/newer request rather than xterm-local scroll calls.
- [x] Protocol accepts only older/newer/latest and one-to-three pages; unknown
  actions close with invalid payload.
- [x] Server re-resolves the opaque session and uses explicit argument arrays
  for `display-message`, `copy-mode`, and `send-keys -X page-up/page-down`.
- [x] WebSocket integration observes clamped tmux control while fake PTY input
  remains zero bytes.
- [x] Connection ownership triggers best-effort Latest cleanup after disconnect
  without canceling a copy mode that predated the app operation.
- [x] Dedicated real tmux socket reports positive `scroll_position` after Older
  and `pane_in_mode=0` after Latest.
- [x] Older and Latest are the first and second stable children of the
  horizontally scrolling bottom shortcut bar, followed by Esc and Tab. Latest
  is disabled only while history mode is inactive.
- [x] Per-connection four-token/four-per-second bucket closes process bursts;
  regression observes four operations, policy close, cleanup, and zero PTY data.
- [x] On 2026-08-02 the owner reported successful iPhone testing of the actively
  deployed build, satisfying the physical-device acceptance gate.

## Verification

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run build
TMUX_MOBILE_RUN_LINUX_INTEGRATION=1 dotnet test \
  tests/TmuxMobile.Infrastructure.Tests/TmuxMobile.Infrastructure.Tests.csproj \
  --no-restore --filter FullyQualifiedName~RealIsolatedTmuxEntersAndExitsCopyMode
./scripts/verify.sh
```

- Frontend direction/protocol tests, typecheck, and production build: pass.
- Real isolated tmux history test: 1 passed and destroyed only its unique socket.
- Canonical gate: 24 Core, 10 Infrastructure, and 16 Server tests passed; two
  opt-in Linux tests skipped in the canonical run; both frontend test files and
  typecheck passed.

```text
container=healthy https_live=200 terminal_bundle=200 startup=clean
listener=100.85.13.102:8780
bundle_order=shortcut_bar:older,latest,esc,tab
bundle_absent=history_banner,terminal_toolbar,pinned_latest_button
```

## Findings

- Blocking code/security findings: none.
- Resolved medium: history actions initially lacked per-connection process-rate
  control. `c3f93be` adds a token bucket and burst regression.
- Resolved device finding: owner reached history mode but reported the trailing
  Latest control looked partially obscured. `81d6d95` moved it outside overflow
  but introduced a separate banner the owner rejected; RCA
  `2026-08-01--latest-banner-layout-miss.md` records the verification gap, and
  `2faa017` then pinned Latest beside the scrolling region, which the owner also
  rejected. `2026-08-01--latest-pinned-layout-mismatch.md` records the clarified
  stable-adjacency contract, and `55c9f70` restored the permanent pair at the
  trailing end. The owner then clarified that the pair must be first;
  `2026-08-01--history-controls-order-mismatch.md` records that ordering gap and
  `1383b83` makes Older/Latest the first two controls. The owner's successful
  iPhone test closes the final device finding.
- Low: tmux copy mode is authoritative pane state, so concurrent attached
  clients may observe history mode briefly. Cleanup is explicit and the MVP is
  single-user, but simultaneous terminal clients can affect one another.
- Low: live checks deliberately did not attach to or control a user session.

## Compatibility and Rollback

- Existing PTY input, paste, shortcuts, resize, heartbeat, reconnect, REST, and
  inventory protocol paths are unchanged.
- Older clients remain compatible because history is an additive WebSocket
  message type; the server rejects only unsupported client messages as before.
- No persistent data or migration exists.
- Rollback: revert `1383b83`, `55c9f70`, `2faa017`, `81d6d95`, `c3f93be`, and
  `0774ab4`, rebuild the Serve profile, and confirm HTTPS health and prior
  bundle. This restores C008's known ineffective scrollback behavior and is
  therefore an emergency rollback, not a resolution.

## Approvals

- Corrective implementation and current test-container deployment: repository
  owner, 2026-08-01.
- Reviewer: Codex, evidence-backed local review.
- Status: ready; required physical-iPhone acceptance received 2026-08-02.
- Merge/push: not authorized or implied.

## Owner Acceptance

- Completed: the owner reported that iPhone testing was successful on
  2026-08-02, confirming the requested device acceptance checklist against the
  actively deployed application.
