# Proposal: Mobile Terminal Clipboard Paste

Date: 2026-08-01
Owner: Human Partner and AI Agent
Risk Class: T2
Related Context: owner requested clipboard paste in terminal mode
Roadmap Item: C007
Planned Branch: `feat/c007-terminal-clipboard-paste`
Expected Commit Count: 2

## Objective

Let the owner paste clipboard text into the interactive xterm.js terminal from
an iPhone with explicit consent, clear safeguards, and a dependable manual
fallback.

## Scope

In scope:

- A visible, accessible Paste button in the terminal shortcut bar.
- Clipboard `readText()` from a user gesture, xterm.js paste transformations,
  and a manual textarea fallback.
- Confirmation for multiline or large text and serialized-message chunking
  below the terminal WebSocket limit.
- Frontend build, automated verification, live app-shell validation, and current
  Tailscale Serve test-container replacement.

Out of scope:

- Clipboard history, background clipboard reads, binary/image paste, server-side
  clipboard storage, automatic Enter, or sending test text to a real user tmux
  session.

## Acceptance Criteria

- [x] Terminal mode exposes a visible Paste button with accessible labeling.
- [x] Single-line text can flow through `Terminal.paste`; denied/unavailable
  clipboard access opens a manual paste field.
- [x] Multiline or large text requires explicit Send confirmation and never
  adds Enter automatically.
- [x] Serialized WebSocket input is chunked below the 16 KiB server limit and
  preserves Unicode code points.
- [x] Clipboard text is cleared after send/cancel and is neither logged nor
  persisted.
- [x] Canonical verification, live app-shell validation, and Change Review pass.

## Verification Plan

```bash
npm --prefix src/TmuxMobile.Web run build
./scripts/verify.sh
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env config --quiet
```

Live validation checks the HTTPS shell, new terminal bundle, container health,
and exact listener without attaching or sending input to a real session.

## Change Review Plan

- Review Boundary: merge into `feat/c006-tailscale-serve-https`
- Planned Review Record: `REVIEWS/2026-08-01--terminal-clipboard-paste.md`

## Decomposition Plan

1. Add bounded input chunking and guarded paste UX.
2. Build and validate the mobile terminal bundle and fallback states.
3. Rebuild the current test container, run canonical verification, and review.

Thin slice: a user tap can paste one clipboard line into xterm.js without an
implicit Enter.

## Rollback Plan

Revert C007, rebuild `compose.tailscale-serve.yaml`, and confirm the prior
terminal shortcut bar and HTTPS health. Persistent application state is
unchanged.

## Risks and Mitigations

- Accidental command execution: never append Enter; require confirmation for
  multiline/large content and use bracketed-paste-aware xterm semantics.
- Clipboard disclosure: read only after a user tap; keep text in ephemeral
  component state; never log or store it.
- Oversized WebSocket input: split JSON messages below a conservative byte cap
  while preserving Unicode code points.
- Live-session impact: validate UI/bundles without pasting into user sessions.

## Approval

- Requested from: repository owner
- Approval status: approved
- Approved at: 2026-08-01 America/Chicago
