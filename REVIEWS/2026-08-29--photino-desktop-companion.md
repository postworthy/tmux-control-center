# Review Record: tmuxctl Photino Desktop Companion

Date: 2026-08-29
Review Boundary: merge from `feat/c022-desktop-photino-client` into `main`
Merge Method: `git merge --no-ff feat/c022-desktop-photino-client`
Risk Class: T2
Related Proposal: `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Decision: not ready

## Branch

- Source branch: `feat/c022-desktop-photino-client`
- Target branch: local `main` at `24077b6` after the owner-approved
  predecessor-stack fast-forward.
- C022 baseline and actual merge base: `24077b6`; the target-boundary diff now
  contains only C022 changes.

## Commits in Scope

- `616eadb` `docs(desktop): define Photino companion goal`
- `dcb5ff9` `feat(desktop): prove protected Photino attachment`
- `16bdc44` `feat(desktop): add native server profiles`
- `7be3911` `feat(desktop): preserve session tab attachments`
- `08241ab` `feat(desktop): add authoritative tmux topology`
- `07b6d95` `feat(desktop): add native terminal interactions`
- `7381dae` `build(desktop): add self-contained source targets`
- `fc315a9` `docs(review): record Photino companion findings`
- `8741bf4` `feat(desktop): negotiate server capabilities`
- `4b0769f` `docs(review): close desktop compatibility finding`
- `15ea494` `docs(desktop): add physical acceptance checklist`
- `047c63f` `docs(goal): record desktop acceptance blocker`

## Git Conformance Checklist

- [x] Source branch matches naming policy.
- [x] No direct C022 commit was made to `main`.
- [x] Commit subjects are conventional.
- [x] Every commit carries the required `Roadmap` and `Proposal` trailers. The
  owner authorized local history repair; rewritten commit `07b6d95` retains its
  `Goal` and `Work-Unit` trailers and now carries both required scope trailers.
- [x] The branch-to-`main` boundary contains only the approved C022
  decomposition. Local `main`, the merge base, and the declared baseline all
  resolve to `24077b6`.

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
  implemented and focused/runtime tested. A real self-signed endpoint proves
  certificate rejection before remote navigation; physical sleep remains part
  of owner acceptance below.
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
git diff --check main..HEAD
git log --format=full main..HEAD
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
- A real Photino probe against a disposable self-signed HTTPS endpoint rejected
  the certificate before remote navigation and rendered explicit TLS
  certificate/URL guidance in the native chooser.
- The owner-approved history repair produced the same pre/post-rewrite HEAD tree
  `e5f790766707e06a8b4a4249a1175991d56f2859`. Every C022 commit has the
  required trailers, and after the predecessor-stack fast-forward
  `git merge-base main HEAD` equals local `main` at `24077b6`.
- The unqualified post-repair canonical attempt exits 145 after its shell
  suites because the host default is SDK 8 while the repository pins .NET 10.
  Re-running with the existing ignored repo-local SDK 10.0.302 exits 0: 27 Core,
  26 Infrastructure plus five intentional skips, 34 Desktop, 51 Server
  integration, all six frontend suites, the shell suites, and both Compose
  assertions pass. No toolchain contract changed.

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
No secret, terminal content, generated package output, installer, publication,
live-session mutation, Tailscale change, auth exception, or caller-controlled
tmux command was found in the reviewed C022 range.

### Resolved after initial review

1. Rewritten commit `8741bf4` resolves the older-server compatibility finding
   with a shared
   protocol model, anonymous content-free rate-limited metadata endpoint, and
   native pre-navigation check. Eight native tests cover exact routing,
   forward-compatible versions, missing capabilities, 404/401, redirect
   refusal, malformed/oversized responses, and sanitized TLS failure; one
   server integration test verifies the exact response fields and cache policy.
2. The owner selected predecessor-stack ordering. Local `main` was
   fast-forwarded from `10b5648` through `005e182`, `dbd9474`, and `24077b6`,
   making the C022 review boundary scope-clean without replaying its earlier
   commits.
3. The owner authorized local history repair. `69b4aeb` was replaced by
   `07b6d95` with the required Roadmap and Proposal trailers, its descendants
   were replayed, and the pre/post-rewrite tree hash remained unchanged.

## Risk, Compatibility, and Rollback

- T2 remains appropriate because the feature creates real PTY attachments and
  adds guarded tmux topology mutations, even though the native shell itself is
  local and server management remains out of scope.
- Server routes are additive; the root mobile PWA source and workspace-recovery
  source are unchanged across `main..HEAD`. Existing auth/origin/CSRF and
  WebSocket controls are reused rather than relaxed.
- No data/schema migration exists. Profile state is versioned, contains only
  ID/label/origin, and can be removed independently.
- Roll back by reverting the C022 commits in reverse order or removing the
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
- Follow `docs/desktop-acceptance.md` for both physical-platform checks and
  report only its sanitized evidence fields.
- After physical acceptance, re-run canonical verification and update this
  Review Record to a final readiness decision. Merge, push, CI, and publication
  remain explicitly owner-controlled.
