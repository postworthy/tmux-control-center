# Goal: tmux-Backed Terminal Scrollback Correction

Status: active
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-01
Proposal: `PROPOSALS/2026-08-01--tmux-backed-terminal-scrollback.md`
Review Boundary: merge from `fix/c009-tmux-backed-scrollback` into
`feat/c008-terminal-touch-scrollback`

## Outcome

Terminal touch gestures navigate tmux-owned history and reliably return to live
output on the target iPhone.

## Acceptance Criteria

- [x] AC1 — Down/up gestures serialize only bounded older/newer page actions.
  - Evidence: frontend direction/clamp protocol tests pass.
- [x] AC2 — The terminal WebSocket routes history actions through tmux control
  without writing PTY input.
  - Evidence: integration test observes the clamped fake tmux call and zero PTY
    input bytes; unknown action closes with invalid payload.
- [x] AC3 — Fixed tmux commands visibly navigate and cancel copy mode.
  - Evidence: exact-argument tests pass; the opt-in dedicated-socket test
    observes positive `scroll_position` after Older and `pane_in_mode=0` after
    Latest.
- [x] AC4 — Disconnect performs best-effort cleanup only when this connection
  entered history mode.
  - Evidence: integration test observes the cleanup Latest action after close;
    server ownership state avoids cleanup for pre-existing copy mode.
- [x] AC5 — Focused and canonical verification pass.
  - Evidence: frontend, infrastructure, server integration, production build,
    real isolated tmux, and `./scripts/verify.sh` pass.
- [ ] AC6 — Live exact-IP deployment and physical-iPhone gesture pass.
  - Evidence: pending deployment and owner confirmation.

## Authority Envelope

### May Continue Without Asking

- Implement and test the bounded correction, rebuild only the existing
  Tailscale Serve test container, and run read-only live checks.

### Must Pause for Approval

- Sending validation input into a user pane, changing tmux configuration/history
  depth, broadening network exposure, changing authentication, merging, or
  pushing.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Protocol | completed | Typed bounded history action crosses WebSocket boundary | frontend/server passed |
| 2. tmux control | completed | Exact safe copy-mode commands and cleanup | infrastructure/real tmux passed |
| 3. Deploy/review | completed | Canonical/live checks and review recorded | verify/HTTPS/review passed |
| 4. Device acceptance | pending | Owner confirms physical-iPhone gesture | owner evidence |

## Progress

- 2026-08-01: owner reported deployed C008 gesture does not work.
- 2026-08-01: RCA identified the xterm-local versus tmux-owned history boundary.
- 2026-08-01: added typed page-history messages, strict parsing/clamping, fixed
  tmux copy-mode commands, ownership-aware cleanup, audits, and documentation.
- 2026-08-01: corrected `cursor-up` to `page-up` after the stronger real-tmux
  viewport assertion exposed that cursor movement alone may not move output.
- 2026-08-01: focused tests and canonical verification passed.
- 2026-08-01: Change Review identified missing per-connection history-operation
  throttling; execution resumed to add a four-operation burst/four-per-second
  token bucket and a WebSocket burst regression before review continues.
- 2026-08-01: committed `0774ab4` and review hardening `c3f93be`, rebuilt the
  Tailscale Serve container, and confirmed healthy HTTPS, clean startup, the
  live corrective bundle, and exact-IP listener.
- 2026-08-01: Change Review recorded technical readiness with the review
  boundary held open for required physical-iPhone acceptance.
- 2026-08-01: owner confirmed history scrolling reaches a scrolled position but
  reported the trailing Latest button looked partially obscured. Device
  acceptance remained open while Latest moved into a dedicated history banner.
- 2026-08-01: owner rejected the dedicated banner as visually disconnected from
  the terminal controls. RCA narrowed the correction to a pinned Latest button
  in the existing shortcut row with no additional visible status tier.
- 2026-08-01: replaced the banner with one grid toolbar containing the scrolling
  shortcut region and a non-scrolling trailing Latest control. Frontend focused
  checks, production build, and canonical verification pass.
- 2026-08-01: rebuilt the existing exact-IP Tailscale Serve test container.
  Container health, HTTP/2 HTTPS health, served same-row bundle markers, clean
  startup, and the exact `100.85.13.102:8780` listener all pass.
- 2026-08-01: owner clarified that Latest must always be inside the bottom
  shortcut bar immediately near Older. RCA found that the pinned sibling layout
  enforced an incorrect interpretation of “same row.”
- 2026-08-01: restored Latest as the persistent button immediately after Older
  inside `.shortcut-bar`; it remains disabled outside history mode. Focused
  frontend checks, production build, structural bundle inspection, and canonical
  verification pass.
- 2026-08-01: rebuilt the existing exact-IP test container. It is healthy,
  Tailscale Serve HTTPS health returns 200, the served bundle contains only the
  shortcut-bar Older/Latest grouping, and the listener remains exactly
  `100.85.13.102:8780`.
- 2026-08-01: owner clarified that Older and Latest must be the first controls
  in the list. RCA found the prior acceptance contract covered adjacency but
  omitted ordering priority.
- 2026-08-01: moved Older and Latest to the first and second `.shortcut-bar`
  positions, followed by Esc and Tab. Frontend focused checks, production build,
  source/bundle inspection, and canonical verification pass.
- 2026-08-01: rebuilt the existing exact-IP test container. It is healthy,
  Tailscale Serve HTTPS health returns 200, the served bundle begins Older,
  Latest, Esc, Tab, and the listener remains exactly `100.85.13.102:8780`.

## Evidence

- `RCA/2026-08-01--xterm-local-scrollback-noop.md`.
- `RCA/2026-08-01--latest-banner-layout-miss.md`.
- `RCA/2026-08-01--latest-pinned-layout-mismatch.md`.
- `RCA/2026-08-01--history-controls-order-mismatch.md`.
- Canonical gate: 24 Core, 10 Infrastructure, and 16 Server tests passed; two
  opt-in Linux tests skipped by the canonical run; frontend unit/typecheck pass.
- WebSocket burst regression: exactly four history operations accepted, fifth
  closed with policy violation, cleanup reached Latest, and PTY input stayed
  empty.
- Explicit opt-in real-tmux history test: 1 passed on a unique socket and
  confirmed visible scroll position plus Latest cleanup.
- Live: container healthy; HTTPS live endpoint and corrective hashed bundle
  returned 200; listener remains exactly `100.85.13.102:8780`.
- Superseded same-row attempt: source and production checks proved Latest was
  outside `.shortcut-bar`; owner feedback established that this was the wrong
  grouping despite passing those checks.
- Clarified bottom-bar correction: source and production bundle render the
  stable Older-then-Latest sequence inside `.shortcut-bar`; Latest uses
  `disabled={!historyMode}` and no `terminal-toolbar`, `has-history`,
  `latest-button`, or `history-banner` layout remains. Frontend
  tests/typecheck/build and the canonical gate pass.
- Live clarified correction: healthy container; HTTP/2 HTTPS health 200; served
  bundle reports `shortcut-bar`, `Older`, and `Latest` with no pinned toolbar
  markers; exact Tailscale-IP listener confirmed.
- First-control correction: source and generated bundle order begins Older,
  Latest, Esc, Tab; Latest remains stable and disabled outside history mode.
  Frontend tests/typecheck/build and the canonical gate pass.
- Live first-control correction: healthy container; HTTP/2 HTTPS health 200;
  served bundle order begins Older, Latest, Esc, Tab; exact Tailscale-IP
  listener confirmed.

## Retry State

- Current corrective attempt: 1 against the clarified ordering contract
- Maximum attempts per unchanged failure: 2
- Prior inferred-placement attempts: exhausted; the owner then supplied the
  exact stable adjacency requirement.
- Last failure: `55c9f70` made Older/Latest stable and adjacent but left them at
  the trailing end rather than first in the list.

## Next Action

- Owner fully reopens the PWA and confirms the bottom bar begins Older, Latest,
  Esc, Tab and Latest returns history mode to live output.

## Pause Conditions

- Pause if correction requires arbitrary commands, tmux configuration changes,
  real-session test input, or expanded network/security authority.
