# Goal: Mobile Terminal Clipboard Paste

Status: completed
Owner: Human Partner and AI Agent
Risk: T2
Updated: 2026-08-01
Proposal: `PROPOSALS/2026-08-01--terminal-clipboard-paste.md`
Review Boundary: merge from `feat/c007-terminal-clipboard-paste` into
`feat/c006-tailscale-serve-https`

## Outcome

The owner can deliberately paste clipboard text into an iPhone terminal session
with xterm semantics, confirmation safeguards, and a manual Safari fallback.

## Non-Goals

- Do not read the clipboard in the background, store clipboard text, append
  Enter, support binary clipboard data, or send validation text to user sessions.

## Acceptance Criteria

- [x] AC1 — Terminal mode has an accessible Paste button.
  - Evidence: built terminal bundle includes the visible control and accessible
    label; TypeScript build passes.
- [x] AC2 — Clipboard text uses xterm paste and fallback works when read fails.
  - Evidence: implementation calls `Terminal.paste`; absent/rejected read opens
    the manual textarea dialog.
- [x] AC3 — Multiline/large text requires confirmation; Enter is never added.
  - Evidence: confirmation predicate tests pass; paste status and dialog state
    explicitly state that Enter is not sent.
- [x] AC4 — Unicode input is serialized in chunks below the server limit.
  - Evidence: dependency-free unit test reconstructs Unicode/control input
    exactly across seven messages, each no larger than 12,000 bytes.
- [x] AC5 — Clipboard data is ephemeral and absent from logs/storage.
  - Evidence: clipboard state is component-local and cleared on send/cancel;
    focused inspection finds no logging, fetch, or browser-storage path.
- [x] AC6 — Canonical verification, live shell validation, and review pass.
  - Evidence: canonical verification passed; deployed HTTPS terminal bundle and
    health returned 200; C007 Review Record is ready with iPhone follow-up.

## Authority Envelope

### May Continue Without Asking

- Implement, build, and test the approved frontend feature.
- Rebuild and replace only the current Tailscale Serve test container, then run
  read-only live shell/bundle/health checks.

### Must Pause for Approval

- Sending validation input to a user tmux session, changing backend limits,
  broadening network exposure, changing tailnet policy, merging, or pushing.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1. Paste UX | completed | Guarded paste path and fallback compile | build/typecheck |
| 2. Safety checks | completed | Byte bounds and ephemeral state verified | focused inspection/build |
| 3. Deploy/review | completed | Live bundle, full gate, review pass | HTTPS/verify/review |

## Progress

- 2026-08-01: owner explicitly requested implementation.
- 2026-08-01: paste button, xterm path, manual fallback, review dialog, 128 KiB
  total cap, and 12,000-byte message chunking implemented.
- 2026-08-01: focused terminal-input unit test and production frontend build
  passed.
- 2026-08-01: canonical verification passed 44 .NET tests with one opt-in PTY
  test skipped, the new terminal-input unit test, TypeScript, and Compose checks.
- 2026-08-01: implementation committed as `af6affd` and deployed from that exact
  commit to the current Tailscale Serve container.
- 2026-08-01: live HTTPS health and hashed terminal bundle returned 200; bundle
  contains the Paste control, fallback, confirmation, and size-limit states.
- 2026-08-01: scope/security review found no blocking or high findings.

## Evidence

- AC1/AC2/AC3: production frontend build exit 0; built bundle contains Paste,
  fallback-dialog, and no-Enter messages.
- AC4: `npm ... run test:unit` — pass, seven bounded chunks and exact round trip.
- AC5: focused source/storage/log inspection — no clipboard persistence/logging.
- AC6: `./scripts/verify.sh` — exit 0; live container healthy, HTTPS/bundle 200,
  exact listener retained, and Review Record ready.

## Discoveries

- Current xterm input already flows through `onData` to authenticated PTY input.
- The server caps complete WebSocket messages at 16,384 bytes.
- Clipboard reads require HTTPS and user activation; Safari may prompt for Paste.

## Decisions

- Use `Terminal.paste` rather than treating clipboard text as key presses.
- Keep each serialized input message at or below 12,000 bytes for headroom.

## Retry State

- Current attempt: 0
- Maximum attempts per unchanged failure: 2
- Last failure: none

## Next Action

- No execution action remains; owner should validate clipboard prompting and
  terminal behavior on the physical iPhone.

## Pause Conditions

- Pause if implementation requires background clipboard permission, server-side
  clipboard storage, a backend limit increase, or real-session test input.

## Outcomes

- Completed without attaching to or sending validation input into a user tmux
  session. Physical iPhone paste prompting remains the final user check.
