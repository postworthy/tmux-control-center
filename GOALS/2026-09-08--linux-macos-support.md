# Goal: Linux and macOS server/desktop support

Status: paused
Owner: Landon and Codex
Risk: T2
Updated: 2026-09-08
Proposal: `PROPOSALS/2026-09-08--linux-macos-server-desktop.md`
Review Boundary: reviewed feature-branch commit/push and MacBook source migration;
merge to main and live deployment remain separate.

## Outcome

Incorporate the MacBook PTY work and approved portability integration, verify,
commit/push to existing origin and migrate the reviewed source back to the MacBook
while preserving local state. Each server controls one local tmux host.

## Non-Goals

- No main merge, force push, live service restart, shell startup changes, secrets
  transfer, Windows/Intel Mac expansion or binary publication.

## Acceptance Criteria

The proposal's AC1–AC9 definitions are authoritative.

- [x] AC1 — clean native builds/publish on Linux and macOS. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC2 — observable PTY resize and lifecycle on both kernels. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC3 — isolated tmux lifecycle/topology/history. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC4 — security and private-storage parity. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [ ] AC5 — native Mac server build/start/service instructions. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC6 — portable optional recovery. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [ ] AC7 — desktop/PWA matrix and physical acceptance. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC8 — platform-aware canonical gates. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
- [x] AC9 — docs/review/rollback and approved migration. Evidence: see criterion mapping in `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.

## Authority Envelope

May continue: owner-approved T2 implementation in C023, local reversible work,
commits, push to existing origin, backed-up source migration to the named MacBook,
isolated verification required for that migration. Approval: 2026-09-08 user request.

Pause for: main merge, production changes, service installation/restart, destructive
history changes, expanded architecture, unclear security/privacy or lost host access.

## Work Units

| Unit | Status | Exit criteria | Verification |
| --- | --- | --- | --- |
| 1 contracts | complete | approved scope and single active goal | manual contract review |
| 2 native PTY/build | complete | shared shim and correct native output | clean build and child resize |
| 3 Unix lifecycle | complete | isolated tmux and process cleanup | Unix integration suite |
| 4 private storage | complete | same protections on Mac/Linux | permission and API tests |
| 5 native delivery | complete | source publish and launchd templates | publish/package checks |
| 6 recovery | complete | portable utilities and locking | recovery suite |
| 7 platform matrix | physical follow-ups | canonical platform gates and available host evidence | verify.sh and builds |
| 8 review/migration | complete | reviewed commits pushed and Mac source reconciled | hashes, Git and backup |

Thin slice: unit 4 permits a private foreground Mac server to serve a real terminal.

## Progress

- 2026-09-08: incoming diff still consists of four tracked files plus documented
  untracked note/helper/build outputs. Source baseline is 8e8cc37; local includes
  clipboard commit d3baaae. Feature branch created; C022 physical acceptance paused.

## Evidence

- Source patch: `PROPOSALS/supporting/2026-09-08--macbook-pty.patch`.
- Baseline Linux canonical gate passed in proposal examination.

## Discoveries

- Owner supplied SSH port 24; passwordless access works. The Mac currently runs
  macOS 26.6.2 arm64, .NET 10.0.400, tmux 3.7b and Homebrew Bash 5.2.37.
- Native .NET/tmux and recovery tests pass on both OSes. macOS required BSD chmod
  syntax, real helper filenames for process classification, and canonical temp
  paths in the recovery test; snapshot format and restore semantics are unchanged.
- Physical Mac desktop startup exposed a pre-initialization resize callback
  calling SendWebMessage. Geometry notifications now require the existing
  desktopReady handshake; chooser/connecting pages do not receive terminal refits.
- Original Mac changes are backed up privately at
  `/Users/landon/code/tmux-control-center-c023-backup.m5WA5Y`; patch SHA-256 matches
  the exact proposal snapshot. Unrelated local helper/note and settings remain.

## Decisions

- Preserve all Mac work in a private backup before replacing incorporated files.
- Preserve current clipboard code; do not import generated Mac desktop assets.

## Retry State

- Current attempt: 1
- Maximum attempts per unchanged failure: 2
- Last failure: resolved. Mac pre-initialization geometry correction passes actual startup; both canonical gates and published-server smoke tests pass.

## Next Action

- Obtain physical client/server-pairing, clipboard/Dock and operator service acceptance for the remaining AC5/AC7 boundaries.

## Pause Conditions

- The authority boundaries above apply; physical acceptance needs host evidence.
- Perform RCA before another correction if the owner reports a failed fix.

## Outcomes

- Source task complete: reviewed commits through d93d17e pushed to origin and
  migrated to the MacBook feature branch; the migrated checkout passes canonical
  verification (166 .NET tests, zero skips, 15 frontend test files).
- Original Mac work is preserved in the private backup and named stash
  aea2ad71272e0755e3c7da70bc14f72bb7ac105b. Personal untracked files/settings remain.
- Verified native server and desktop artifacts are available on the Mac under
  artifacts/c023-20260908-d93d17e. Final documentation sync follows this checkpoint.
- Full goal completion remains paused at physical matrix and live service
  acceptance; C022 is not marked complete. No production rollout occurred.
