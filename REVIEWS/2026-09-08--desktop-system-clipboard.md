# Review Record: Desktop system clipboard

Date: 2026-09-08
Review Boundary: merge `fix/desktop-system-clipboard` into `main`; production
deployment remains a separate owner-approved action.
Risk Class: T1
Related Proposal: `PROPOSALS/2026-08-29--photino-desktop-companion.md`, AC6
clipboard correction explicitly requested by the owner in this session.

## Scope and decision

Ready with explicit follow-ups. Reviewed the uncommitted change against
`8e8cc3702f47b5b7a5197bb0325c5ecad33ff0b8`; no merge or deployment is approved by
this record. Scope is `DesktopTerminal.tsx`, new `terminalClipboard.ts` and its
test file, README, desktop acceptance guidance, the active C022 checkpoint,
and this review. No unrelated work was present at preflight. No direct commit
to main, dependency change, generated bundle, or secret enters the source diff.

## Behavior and evidence

- Mouse-release copying uses the finalized xterm selection. Explicit
  Ctrl+Shift+C and Command+C use the same writer; ordinary Ctrl+C still reaches
  the terminal. Listeners and the OSC handler are disposed on terminal teardown.
- OSC 52 decodes tmux copied text into a bounded UTF-8 system-clipboard write.
  Read queries, invalid targets/base64/UTF-8, empty and oversized payloads are
  ignored. Inactive tabs and unfocused windows cannot write from terminal output.
- A temporary textarea uses WebKit's copy command before asynchronous clipboard
  access, restores focus, and is removed even on failure. Clipboard text is not
  logged, persisted, interpreted as markup, or returned to the remote session.
- Focused tests cover Unicode/multiline text, primary/default clipboard targets,
  query rejection, byte boundaries, absent/denied APIs, synchronous success,
  asynchronous fallback, throwing copy commands, and empty selections.
- `npm --prefix src/TmuxMobile.Web run test:unit`: 15 test files passed.
- `npm --prefix src/TmuxMobile.Web run typecheck`: passed.
- `npm --prefix src/TmuxMobile.Web run build:desktop`: passed; existing bundle
  size warning only. Built assets are retained outside source under ignored
  `artifacts/desktop-system-clipboard-20260908/desktop/` for rollout review.
- `PATH="$PWD/.dotnet:$PATH" ./scripts/verify.sh`: final exit 0, including .NET,
  frontend, shell, and Compose checks. Existing opt-in tmux tests remain skipped
  by the canonical command; the clipboard experiment below used real tmux.
- Isolated GTK/WebKit 4.1 on Xvfb, with browser permission requests denied:
  actual XTest button copy and programmatic copy both produced exact Unicode
  text read by a separate xclip process from CLIPBOARD (not PRIMARY).
- Disposable real tmux on a dedicated `/tmp` socket, with TERM=xterm-256color:
  copy-mode selection emitted OSC 52 containing the expected harmless sentinel.
  That captured payload passed the decoder/WebKit writer/external xclip chain.
  Local harnesses and backups are in `/tmp/tmuxctl-clipboard-check/`.
- Read-only inspection of live tmux reported `set-clipboard external`. No live
  session input, host setting change, service restart, or clipboard access on
  the owner's real display was used in testing.
- `git diff --check`: passed.

## Risks, rollback, and follow-ups

- OSC 52 allows output in a focused active terminal to replace the clipboard,
  as in conventional terminals. The operation is write-only and bounded to
  128 KiB; Photino's general browser-permission policy remains unchanged.
- WebKit copy-command behavior was observed on Linux; physical macOS acceptance
  remains pending. If a platform rejects both copy mechanisms, a visible error
  directs the owner to explicit selection/copy.
- The native app loads server-hosted desktop assets. Production deployment and
  the owner's actual mouse-selection/external-paste acceptance remain pending.
  This review does not mark the wider C022 goal complete.
- Rollback: revert these scoped source changes and rebuild the previous desktop
  assets. No data migration, native binary change, or tmux setting reversal is
  needed. Run `./scripts/verify.sh` using the repository's local SDK afterward.

Reviewer: Codex (self-review). Merge approval remains pending.

## Deployment evidence (subsequent owner approval)

The owner explicitly authorized `deploy` on 2026-09-08, superseding the pending
deployment boundary above. Image `tmux-mobile:desktop-system-clipboard-20260908`
(`sha256:efdd5ec8aa763a7e87f2b5796ab71dfd82d2e40a05c64fd6a48bab71f547750d`)
adds exactly one desktop-assets layer to the prior running image. Image runtime
configuration is identical and rendered Compose differs only by image tag.
Only the app service was recreated. All three deployed desktop files matched
the tested artifacts byte-for-byte over HTTPS. Desktop HTML remains no-store;
HTTPS health returned 200 and direct HTTP remained denied with 426. All six
tmux pane identities and process IDs survived unchanged. The prior image is
retained as `tmux-mobile:pre-system-clipboard-20260908`, and the ignored
deployment settings now select the new image persistently. Owner interaction
acceptance after reopening the desktop app remains pending.
Final Docker health check: healthy, zero restarts.

## Pre-commit verification

The owner requested committing all ready local changes on 2026-09-08.
Rechecked the eight changed/new source, test, and documentation files on
`fix/desktop-system-clipboard` against `8e8cc37`; no unrelated files or generated
artifacts are included. `PATH="$PWD/.dotnet:$PATH" ./scripts/verify.sh` passed
again: 158 .NET tests passed, six opt-in tests skipped, all 15 frontend test
files passed, and typecheck, shell, delivery, and Compose checks passed.
`git diff --check` also passed. Ready for the requested local commit; physical
clipboard acceptance and merge approval remain pending.
