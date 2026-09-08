# Review Record: Linux and macOS server/desktop integration

Date: 2026-09-08
Risk Class: T2
Source branch: `feat/c023-linux-macos-support`
Baseline: `d3baaae` (includes the independent desktop clipboard fix)
Proposal: `PROPOSALS/2026-09-08--linux-macos-server-desktop.md`
Reviewer: Codex (self-review)
Boundary: owner-authorized feature-branch commit/push and backed-up MacBook source
migration. Main merge and production service changes are not performed.

## Decision

Ready with explicit follow-ups for source integration and migration. No blocking
source finding remains from this review. Full physical desktop/server pairing,
Dock/clipboard acceptance, and production launchd/Tailscale validation remain
open; this is not a declaration of completed platform rollout or C022 acceptance.
The owner explicitly authorized implementation, commits, push and Mac source
migration on 2026-09-08, and supplied passwordless SSH port 24 for host validation.

## Scope and findings

- Incorporates the four-file Mac PTY change, preserving its original patch as
  review evidence. Mac work was based on 8e8cc37 and does not overlap d3baaae's
  clipboard source. No generated library, frontend bundle, runtime settings,
  secret, local note or personal shell auto-attach helper is committed.
- Native resize uses a fixed-signature C wrapper and platform headers/constants.
  `UnixPseudoTerminal` retains fixed argv, size limits and owned process cleanup.
- Native build paths resolve after SDK imports, segregate host/configuration/TFM
  outputs and propagate the compiled library into tests/server/publish outputs.
  Both the server and infrastructure reject a mismatched requested RID; the
  server guard is needed because project-reference builds can omit the RID.
- Linux/macOS now share audit and recovery private-file creation/validation.
  New key directories are private; existing key-directory modes are preserved
  and operator checks are documented. Local/runtime state is excluded from native
  server publish content. Authentication and protocol contracts remain unchanged.
- Recovery uses narrow BSD/GNU utility adapters and a tiny host-compiled flock
  helper on macOS. It keeps snapshot version 1, explicit restore, private state,
  fixed agent-resume operations, Linux flock support and single-daemon behavior.
  Existing Linux helper installations must install the new adjacent
  `workspace-platform.sh` alongside the main helper; docs include that step.
- Native server publishing builds current mobile and desktop assets. Launchd
  templates contain no credentials, use explicit paths/private umask and abandon
  the process group so supervisor exit does not own the tmux server lifecycle.
- Actual Mac desktop launch caught an ApplicationException: AppKit fired resize
  during Photino initialization and `SendWebMessage` was called too early. The
  callback now requires the existing page-originated `desktopReady` handshake,
  resets on navigation, and triggers a refit when the page becomes ready. The
  unchanged build reproduced the failure; the corrected build survives startup
  on both hosts. No dependency update or browser-permission relaxation was needed.

## Verification and criterion evidence

Final Linux environment: x64, repository .NET 10.0.302 SDK, tmux 3.4.
Final Mac environment: Apple Silicon, macOS 26.6.2, .NET 10.0.400, tmux 3.7b,
Homebrew Bash 5.2.37. The Mac's original note described macOS 15/tmux 3.5a;
those older-version claims were not independently reproduced.

| Criterion | Observed evidence | Remaining boundary |
| --- | --- | --- |
| AC1 native delivery | Fresh source snapshots publish self-contained Linux/Mac servers; ELF x64 `.so` and Mach-O arm64 `.dylib` are present; no source-root native output in clean snapshots; published PTY loads and runs | Other OS/architecture/version combinations unclaimed |
| AC2 PTY | Initial, repeated, maximum and restored dimensions observed by child; Unicode/natural exit and forced group cleanup tests pass | None for tested hosts |
| AC3 tmux | Real isolated history, topology, mouse forwarding and detach tests pass; published-server WebSocket resizes tmux to 119x45; shutdown leaves session alive and removes owned client | Full physical UI matrix remains AC7 |
| AC4 security | 58 server integration tests plus audit/private storage tests pass on both; published process rejects anonymous API and unforwarded HTTP, authenticates a generated test credential, creates private audit/key state | Existing production state/ACLs require operator checks |
| AC5 Mac server | Clean native publish starts outside checkout and passes health/readiness, auth, assets, real PTY and shutdown smoke; launchd plists parse | Installing/loading launchd and live Tailscale acceptance not exercised |
| AC6 recovery | Snapshot/restore of isolated 1-session/2-window/3-pane workspace; fixed Codex/Claude resumes, live-session refusal, corrupt-state rejection, private modes, second-daemon exclusion and stop preservation pass on both | Production service setup remains separate |
| AC7 desktop | Both native packages build; actual Mac process and Linux/Xvfb process survive startup with isolated profiles after regression fix | Dock, physical clipboard, sleep/reconnect and all four client/server pairings remain |
| AC8 gates | Canonical command exits 0 on both hosts: 166 .NET tests (32 Core, 42 Desktop, 34 Infrastructure, 58 Server), no skips; 15 frontend files pass; Linux setup/watchdog/Compose remain mandatory; Mac reports exclusions explicitly | None for tested gates |
| AC9 review/migration | Review/provenance/backup and rollback plan complete; commit/push/migration recorded in subsequent checkpoint | Completed in the source-handoff checkpoint below |

Commands:

```bash
PATH="$PWD/.dotnet:$PATH" ./scripts/verify.sh # Linux
PATH="/opt/homebrew/bin:$PATH" bash scripts/verify.sh # Mac
./scripts/build-server.sh
./scripts/build-desktop.sh linux-x64
./scripts/build-desktop.sh osx-arm64
python3 tests/native-server-smoke.py /ABSOLUTE/PUBLISH /ABSOLUTE/TMUX
bash tests/native-desktop-launch.sh /ABSOLUTE/DESKTOP-EXECUTABLE
```

Linux desktop launch used Xvfb outside the filesystem/process sandbox because the
sandboxed app could not connect to its temporary X display. No real desktop
profile or live terminal session was used. The Mac launch used an isolated
`TMUXCTL_CONFIG_HOME` and no server URL. Smoke tests bind loopback and use dedicated
tmux sockets/private temporary state; production instances were not stopped.

Verification logs are local evidence, not source: Linux `/tmp/c023-verify-linux.log`,
`/tmp/c023-build-linux-final.log`, `/tmp/c023-server-smoke-linux-final.log`; Mac
`/tmp/tmuxctl-c023-final-verify.log`, `/tmp/tmuxctl-c023-final-build.log`,
`/tmp/tmuxctl-c023-final-server-smoke.log`,
`/tmp/tmuxctl-c023-final-desktop-smoke.log`.

`dotnet msbuild src/TmuxMobile.Server/TmuxMobile.Server.csproj
-target:ValidateNativeServerTarget -property:RuntimeIdentifier=osx-arm64` on Linux
fails intentionally with the target/host mismatch message. `git diff --check`
passes; captured patch context whitespace is treated as patch data via the
scoped `.gitattributes` rule.

## Mac migration and rollback

Original Mac HEAD: 8e8cc37. Private backup:
`/Users/landon/code/tmux-control-center-c023-backup.m5WA5Y` contains original HEAD,
status, exact tracked patch, source and the documented untracked note/helper/build
outputs, with SHA256SUMS. The patch hash remains
`caab0695cb86dfbf2a76906220199165b21d30251c1bebef90cdae87440f6c83`.

After pushing the reviewed branch, preserve the tracked Mac changes in a named
Git stash as an additional recovery point, then check out the exact pushed commit.
Keep unrelated local files and ignored settings; do not apply the old patch over
the newly integrated implementation. Verify matching Git/source hashes and run
the Mac gate. A Git bundle may transfer the same reviewed commits if the Mac lacks
GitHub SSH credentials; it must not introduce a different source revision.

Rollback restores the saved Mac branch/HEAD and original patch from the private
backup or named stash, without deleting unrelated files/settings. Linux rollback
reverts C023 commits while retaining d3baaae, rebuilds prior assets and runs the
Linux gate. No schema migration is involved. Production deployment must preserve
its own prior publish/configuration and validate session survival separately.

## Completed source handoff

Reviewed commits above d3baaae:

- `14c845c` — approved contracts and original Mac patch evidence.
- `4d2a5df` — shared Unix PTY, private storage and native tests.
- `1a8b38d` — native server delivery, launchd templates and portable recovery.
- `8210ab1` — desktop geometry initialization regression correction.
- `d93d17e` — dual-platform review/evidence checkpoint.

The branch was pushed to the existing GitHub origin and the Mac fetched the exact
`d93d17e5dcc374b90b6c8bbf04d352bd304d51ec` revision over its existing HTTPS origin.
The Mac's original four tracked edits were preserved in named stash
`aea2ad71272e0755e3c7da70bc14f72bb7ac105b`, also recorded in the private backup.
Its single-branch clone originally fetched only main; an additional fetch mapping
for C023 was required to configure normal tracking. The failed initial tracking
setup left the index at the desired target; this was verified and then completed
without reset, overwrite or history rewrite.

The Mac now checks out `feat/c023-linux-macos-support`, tracks its origin branch,
and has no modified tracked files. `MACOS-PTY-FIX.md` and
`scripts/auto-tmux-session.sh` remain as the owner's untracked local files.
Existing ignored settings/state/builds were preserved. Canonical verification
passed again in the migrated checkout, including 166 .NET tests with zero skips
and 15 frontend test files; log: `/tmp/tmuxctl-c023-migrated-verify.log` on the Mac.

Verified source-built Mac artifacts were copied to
`/Users/landon/code/tmux-control-center/artifacts/c023-20260908-d93d17e/`:
`server/` is the self-contained native server and `tmuxctl.app` is the desktop
bundle. These are ignored local artifacts, not published Git binaries.
No production service was restarted, replaced, or installed. C023 source handoff
is complete; the goal pauses at physical/service acceptance. This documentation
checkpoint is pushed and synchronized afterward with unchanged application code.
