# Review Record: Mobile Terminal Clipboard Paste

Date: 2026-08-01
Review Boundary: merge from `feat/c007-terminal-clipboard-paste` into
`feat/c006-tailscale-serve-https`
Merge Method: `git merge --no-ff feat/c007-terminal-clipboard-paste`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-01--terminal-clipboard-paste.md`

## Decision

Ready with explicit physical-iPhone follow-up. The deployed terminal now offers
guarded text paste with xterm semantics, a manual Safari fallback, multiline and
large-paste review, and bounded WebSocket messages. No validation input was sent
to a user tmux session.

## Commits in Scope

- `af6affd` feat(terminal): add guarded clipboard paste

The review-record commit is documentation-only evidence created afterward.

## Scope and Git

- [x] Work is on the approved C007 feature branch.
- [x] Diff contains only paste UX, input bounds/tests, built PWA assets,
  documentation, and Tempo records.
- [x] No authentication, authorization, backend limit, network, or tailnet
  configuration changed.
- [x] Ignored test output, local environment, credentials, logs, and runtime
  state are absent from the diff.
- [x] Only the approved Tailscale Serve test container was rebuilt.

## Acceptance Evidence

- [x] Shortcut bar exposes a 48-by-46-pixel minimum Paste target with an
  accessible label and disabled disconnected state.
- [x] Clipboard read occurs only in the Paste click handler.
- [x] Successful text uses `Terminal.paste`, retaining xterm paste
  transformations and bracketed-paste behavior.
- [x] Missing/rejected Clipboard API opens an auto-focused manual textarea.
- [x] Multiline or greater-than-1-KiB text opens a review dialog; no Enter is
  appended.
- [x] Total paste input is capped at 128 KiB.
- [x] Serialized messages are capped at 12,000 bytes below the 16,384-byte
  server limit and preserve Unicode code points.
- [x] Paste state clears after send/cancel and has no logging/storage path.
- [x] The live hashed bundle contains every expected Paste/fallback safety state.

## Verification

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
```

- Terminal-input test: pass; hostile Unicode/control/JSON content reconstructed
  exactly across seven messages, every message at or below 12,000 bytes.
- Frontend production build: pass.
- Canonical gate: 24 Core, 7 Infrastructure, and 13 Server tests passed; one
  opt-in isolated PTY test skipped; TypeScript, terminal-input, and production
  Compose checks passed.

```text
container=healthy https_live=200 terminal_bundle=200
listener=100.85.13.102:8780
bundle_states=paste_button,fallback,confirmation,size_limit
```

## Findings

- Blocking/high: none.
- Medium: clipboard paste can change terminal state. The UI never adds Enter,
  uses xterm paste semantics, and requires explicit confirmation for multiline
  or large input, but the owner must still review commands before sending.
- Low: Safari can show an OS-controlled Paste prompt or deny programmatic read;
  the manual textarea is the supported fallback.
- Low: automated/live checks deliberately did not open a real terminal or send
  input. Physical-iPhone terminal verification remains required.

## Compatibility and Rollback

- Existing keyboard and shortcut input now uses the same bounded serializer;
  short input remains one identical JSON message.
- REST, WebSocket schema, PTY lifecycle, server limits, and deployment settings
  are unchanged.
- Rollback: revert C007, rebuild the Serve profile, and verify HTTPS health and
  the prior terminal bundle. No persistent data migration exists.

## Approvals

- Feature implementation and current test-container deployment: repository
  owner, 2026-08-01.
- Reviewer: Codex, evidence-backed local review.
- Status: ready with physical-iPhone follow-up.
- Merge/push: not authorized or implied.

## Follow-Ups

- On the iPhone, test a short single line, a multiline confirmation, and the
  manual fallback without choosing sensitive clipboard contents.
- Replace the temporary weak login password as already recorded by C006.
