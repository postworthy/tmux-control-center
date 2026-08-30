# Review Record: tmuxctl Photino Desktop Companion

Date: 2026-08-29
Review Boundary: merge from `feat/c022-desktop-photino-client` into `main`
Merge Method: `git merge --no-ff feat/c022-desktop-photino-client`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Decision: not ready

## Branch

- Source branch: `feat/c022-desktop-photino-client`
- Target branch: `main` at `10b5648`
- C022 baseline: `24077b6`; reviewed C022 range is `24077b6..66372ef`.
- Actual merge base: `10b5648`. The target-boundary diff also contains the
  earlier `005e182`, `dbd9474`, and `24077b6` changes.

## Commits in Scope

- `616eadb` `docs(desktop): define Photino companion goal`
- `dcb5ff9` `feat(desktop): prove protected Photino attachment`
- `16bdc44` `feat(desktop): add native server profiles`
- `7be3911` `feat(desktop): preserve session tab attachments`
- `08241ab` `feat(desktop): add authoritative tmux topology`
- `69b4aeb` `feat(desktop): add native terminal interactions`
- `ba103de` `build(desktop): add self-contained source targets`
- `cbd5414` `docs(review): record Photino companion findings`
- `66372ef` `feat(desktop): negotiate server capabilities`

## Git Conformance Checklist

- [x] Source branch matches naming policy.
- [x] No direct C022 commit was made to `main`.
- [x] Commit subjects are conventional.
- [ ] Every commit carries the required `Roadmap` and `Proposal` trailers.
  `69b4aeb` carries only `Goal` and `Work-Unit` trailers.
- [ ] The branch-to-`main` boundary contains only the approved C022
  decomposition. Three earlier stacked commits are also in `main..HEAD`.

## Change Summary

- Adds a self-contained .NET 10/Photino launcher with owner-only, metadata-only
  server profiles and a strict native message boundary.
- Adds a server-hosted desktop React/xterm.js entry point that reuses existing
  same-origin authentication, CSRF, REST, inventory, and terminal WebSockets.
- Adds real attachment-aware session tabs, native pop-out windows, bounded
  reconnect, conventional desktop shortcuts, guarded clipboard paths, and
  ordinary tmux `exit`/detach/explicit-kill distinctions.
- Adds opaque authoritative window/pane models and a closed set of fixed tmux
  create/select/split/resize/close operations with authorization, rate limits,
  CSRF, strict JSON, stale-target handling, and audit coverage.
- Adds reproducible `linux-x64` and `osx-arm64` source-build commands while
  leaving installers, signing, server launch, and publication out of scope.
- Adds a shared version-1 desktop capability contract and a bounded native
  preflight that refuses redirects and incompatible/older servers before any
  remote UI is loaded.

## Acceptance Checklist

- [x] AC2 implementation: URL/profile validation, compatibility preflight,
  protected same-origin login,
  owner-only metadata storage, offline watchdog, and bounded reconnect are
  implemented and focused/runtime tested. Physical sleep and invalid-TLS
  behavior remain part of owner acceptance below.
- [x] AC3: isolated real-tmux evidence proves one owned client per open tab,
  clean and abrupt detach, network-loss cleanup, reconnect, session survival,
  and preservation of another app-owned client.
- [x] AC4: opaque authoritative session/window/pane topology and every closed
  operation passed focused server tests plus an isolated tmux/Photino round
  trip, including final-pane/window refusal and corrected resize ordering.
- [x] AC5: isolated runtime evidence distinguishes tab detach, ordinary shell
  `exit`, wrong-name refusal, and exact-name session kill.
- [ ] AC1: both source outputs build and Linux launches without installed .NET,
  but no actual Apple Silicon machine or approved macOS CI runner has launched
  the `osx-arm64` output.
- [ ] AC6: synthetic Ubuntu/Xvfb checks cover native pop-outs, multiple real
  clients, tab cycling, detach shortcut, `exit`, and named kill. Owner Ubuntu
  acceptance and direct selection/copy/paste/context-menu/sleep checks remain.
- [ ] AC7: security, focused tests, dependency/license review, canonical
  verification, docs, and rollback inspection pass, but the review/merge
  boundary findings below prevent the Change Review from passing.

## Verification Evidence

Commands run:

```bash
dotnet test tests/TmuxCtl.Desktop.Tests/TmuxCtl.Desktop.Tests.csproj
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run test:unit
scripts/build-desktop.sh linux-x64 /tmp/tmuxctl-c022-publish-linux-x64
scripts/build-desktop.sh osx-arm64 /tmp/tmuxctl-c022-publish-osx-arm64
DOTNET_ROOT=/nonexistent DOTNET_MULTILEVEL_LOOKUP=0 \
  /tmp/tmuxctl-c022-publish-linux-x64/tmuxctl http://localhost:55479
dotnet list TmuxMobile.sln package --vulnerable --include-transitive
npm --prefix src/TmuxMobile.Web audit --omit=dev
./scripts/verify.sh
git diff --check 24077b6..66372ef
git log --format=full 24077b6..66372ef
git log --oneline main..HEAD
```

Results:

- 34/34 desktop tests and all six frontend suites pass.
- The canonical command exits 0: shell setup/watchdog/recovery suites, 27 Core,
  26 Infrastructure plus five intentional opt-in skips, 51 Server integration,
  34 Desktop, six frontend suites, and Compose positive/fail-closed assertions.
- Current self-contained outputs are 80 MiB `linux-x64` and 84 MiB
  `osx-arm64`. File inspection reports the expected x86-64 ELF and arm64 Mach-O
  launchers; Linux Photino linkage has no missing dependency. The Linux bundle
  rendered current server inventory with no usable installed .NET root.
- NuGet reports no vulnerable direct/transitive packages across all eight
  projects; production npm dependencies report zero vulnerabilities. Photino
  4.0.16 and Apache-2.0 are recorded in `THIRD_PARTY_NOTICES.md`.
- Isolated runtime evidence and cleanup details are recorded in
  `GOALS/2026-08-29--photino-desktop-companion.md`.
- A real Photino old-server probe received 404 from the exact compatibility
  endpoint, loaded no remote page, and rendered an explicit server-update
  message. An isolated current image returned protocol 1, after which Photino
  loaded `/desktop/` and established its inventory WebSocket.

## Findings

### Blocking

1. AC1 lacks the proposal-required actual Apple Silicon launch/connection
   evidence. The Linux host can validate the artifact format but cannot prove
   macOS WebKit/Photino startup. This requires separately approved Mac hardware
   or macOS CI.
2. AC6 lacks owner Ubuntu acceptance, including direct focus, selection,
   clipboard copy/paste, context menu, resize, sleep/wake, and mouse/keyboard
   feel. Synthetic Xvfb evidence is useful but cannot approve the subjective
   desktop terminal criterion.
3. The declared merge boundary is not scope-clean. `git log main..HEAD` includes
   the non-C022 commits `005e182`, `dbd9474`, and `24077b6`. Merge C022 only
   after the owner chooses a stacked merge order or authorizes a clean C022
   branch based on the intended target.

### Non-blocking conformance

1. Commit `69b4aeb` is missing the required `Roadmap` and `Proposal` trailers.
   Correct the local history or explicitly waive this metadata requirement
   before crossing the review boundary.

No secret, terminal content, generated package output, installer, publication,
live-session mutation, Tailscale change, auth exception, or caller-controlled
tmux command was found in the reviewed C022 range.

### Resolved after initial review

1. `66372ef` resolves the older-server compatibility finding with a shared
   protocol model, anonymous content-free rate-limited metadata endpoint, and
   native pre-navigation check. Eight native tests cover exact routing,
   forward-compatible versions, missing capabilities, 404/401, redirect
   refusal, malformed/oversized responses, and sanitized TLS failure; one
   server integration test verifies the exact response fields and cache policy.

## Risk, Compatibility, and Rollback

- T2 remains appropriate because the feature creates real PTY attachments and
  adds guarded tmux topology mutations, even though the native shell itself is
  local and server management remains out of scope.
- Server routes are additive; the root mobile PWA source and workspace-recovery
  source are unchanged across `24077b6..66372ef`. Existing auth/origin/CSRF and
  WebSocket controls are reused rather than relaxed.
- No data/schema migration exists. Profile state is versioned, contains only
  ID/label/origin, and can be removed independently.
- Roll back by reverting the nine C022 commits in reverse order or removing the
  additive desktop project/assets/routes and topology operations, then run
  `./scripts/verify.sh` and a mobile attach/detach/session-action smoke test.
  Do not remove or alter live tmux sessions as part of rollback.

## Approvals

- Reviewer: Codex, evidence-backed local Change Review
- Approval status: not approved
- Timestamp: 2026-08-29 19:20 America/Chicago
- Boundary approver: repository owner, pending after blocking findings close

## Follow-Ups

- Obtain actual Apple Silicon launch/connection evidence.
- Complete owner Ubuntu desktop interaction acceptance.
- Resolve the stacked branch boundary and missing commit trailers.
- Re-run focused and canonical verification, then update this Review Record to
  a new readiness decision. Merge, push, CI, and publication remain explicitly
  owner-controlled.
