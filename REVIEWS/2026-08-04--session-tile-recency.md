# Review Record: Session Tiles Ordered by In-App Recency

Date: 2026-08-04
Review Boundary: merge from `feat/c013-session-recency` into `main`
Merge Method: `git merge --no-ff feat/c013-session-recency`
Risk Class: T1
Related Proposal: `PROPOSALS/2026-08-04--session-tile-recency.md`

## Decision

Pending owner acceptance. Local implementation, canonical verification,
production packaging, and diff review pass. No merge, deployment, push, or
publication is authorized by this record.

## Scope and Compatibility

- [x] Terminal actions promote opaque session IDs into device-local MRU order.
- [x] Ranked sessions lead and unranked sessions retain stable server order.
- [x] Malformed storage, duplicates, and stale IDs cannot hide or duplicate live
  sessions.
- [x] Tiles, previous/next navigation, selection, terminal lookup, and rail use
  one derived ordered list.
- [x] No backend, WebSocket, tmux, authentication, or network contract changes.
- [x] Canonical verification passes.
- [ ] Owner accepts the return-to-top behavior on the target iPhone.

## Evidence

- Frontend unit tests: pass, including promotion, repeated promotion, stable
  fallback order, immutability, stale/duplicate IDs, malformed JSON, reload, and
  unavailable storage.
- Frontend typecheck: pass.
- Source inspection: Terminal action updates recency and active ID before opening
  terminal mode; returning remounts the deck at position zero with the promoted
  session first.
- Canonical `./scripts/verify.sh`: pass with 24 Core, 12 Infrastructure (four
  isolated tests skipped), 33 Server integration, all three frontend suites,
  and Compose validation.
- Production image `sha256:e1e79f...`: pass; the clean main bundle contains the
  recency storage key and the service worker is release-stamped.

## Risk and Rollback

- Stored values contain opaque session IDs only, matching the existing local
  active-session identifier; no names, commands, previews, or terminal content
  are persisted.
- Ordering is frontend-only. Reverting C013 returns immediately to server order;
  the unused local-storage key is inert.

## Findings

- Blocking: owner acceptance remains.
- Non-blocking: none recorded.
