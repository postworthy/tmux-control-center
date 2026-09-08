# Proposal: Linux and macOS server and desktop support

Date: 2026-09-08
Owner: Landon
Status: Approved — implementation, commit, push and MacBook source migration authorized
Risk Class: T2 (native interop, server platform, private storage, service lifecycle)
Roadmap Item: C023
Planned Branch: `feat/c023-linux-macos-support`
Proposal Branch: `docs/c023-linux-macos-proposal`
Expected Commit Count: 8 coherent implementation/documentation commits

## Objective

Support the tmuxctl server and desktop client from one source tree on Ubuntu
Linux x64 and Apple Silicon macOS, preserving Linux behavior and the existing
HTTP/WebSocket, authentication, terminal, and workspace-recovery contracts.
A desktop on either OS can connect to a server on either OS; each server still
controls only its own local tmux host.

Adopt the MacBook's focused native PTY fix first, then close the packaging,
permissions, service, and verification gaps needed for supported macOS operation.
The initial macOS acceptance target is macOS 15 / arm64, the environment reported
in the source checkout. Broader macOS version support requires separate evidence.

## Comparison and evidence provenance

Read-only source checkout: `/home/landon/work-laptop-mnt/tmux-control-center`
(SSHFS to the owner's MacBook). Comparison date: 2026-09-08.

- MacBook HEAD: `8e8cc3702f47b5b7a5197bb0325c5ecad33ff0b8`.
- Local comparison HEAD: `d3baaae`, which adds the desktop system clipboard fix
  to that same baseline. The four MacBook modified tracked files have no overlap
  with the eight files in the local clipboard commit.
- MacBook tracked diff: four files, 109 additions and 11 deletions. The source
  changes are uncommitted; there is no macOS commit to cherry-pick.
- Exact captured diff: [MacBook PTY patch](supporting/2026-09-08--macbook-pty.patch).
  SHA-256: `caab0695cb86dfbf2a76906220199165b21d30251c1bebef90cdae87440f6c83`.
  This patch also represents the incoming changes against local `d3baaae`, because
  those four local files still match the shared baseline.
- `git apply --check PROPOSALS/supporting/2026-09-08--macbook-pty.patch`
  passes against the local checkout. This establishes textual applicability,
  not runtime compatibility. No patch was applied to application source.
- `cc -fsyntax-only -Wall -Wextra -Werror` on the MacBook C source passes on
  this Linux host. No macOS compiler or runtime was exercised here.

| MacBook path | Difference from current project | Proposed disposition |
| --- | --- | --- |
| `src/TmuxMobile.Infrastructure/LinuxPseudoTerminal.cs` | Allows Linux/macOS; replaces Linux ioctl constant and direct variadic P/Invoke with native resize wrapper; reports errno | Incorporate; retain bounded size and cleanup behavior |
| `src/TmuxMobile.Infrastructure/native/tmux_mobile_pty.c` | Apple `util.h`, local winsize copy for forkpty, fixed-signature resize wrapper using native TIOCSWINSZ | Incorporate shared C implementation |
| `src/TmuxMobile.Infrastructure/TmuxMobile.Infrastructure.csproj` | Host-conditioned `.so` / `.dylib` build and output/publish copying | Adapt, with correct intermediate paths and host/target validation |
| `tests/TmuxMobile.Infrastructure.Tests/LinuxPseudoTerminalTests.cs` | Default Unix PTY test observes initial and resized dimensions inside a shell | Incorporate and strengthen bounded read/EOF diagnostics |
| `MACOS-PTY-FIX.md` (untracked) | Explains Apple Silicon resize failure; records macOS test and interactive results | Preserve relevant diagnosis/provenance in integration review and docs; do not treat reported results as fresh verification |
| `scripts/auto-tmux-session.sh` (untracked) | Replaces each interactive shell with a newly named tmux session | Exclude: optional shell workflow, unrelated to OS compatibility; do not edit shell startup files |
| `src/TmuxMobile.Infrastructure/libtmuxmobilepty.dylib` (untracked) | Compiled native library | Exclude; rebuild from reviewed source and ignore build outputs |
| `src/TmuxMobile.Server/wwwroot/desktop/` (untracked) | Generated index, `index-C5fgyXkz.js`, `index-HeZ-p1xA.css` | Exclude; regenerate from current frontend source, including `d3baaae` clipboard changes |

No tracked changes were found in the MacBook's Photino desktop project, desktop
build script, or frontend source relative to the shared baseline. The baseline
already targets `linux-x64` and `osx-arm64` and builds a macOS `.app` with an icon.
The incoming patch fixes server-side terminal behavior used by both the PWA and
desktop client; it is not a separate desktop implementation.

The MacBook note reports 159 passing .NET tests and a working 119x45 tmux client
on macOS 15, .NET 10, Homebrew tmux 3.5a. It says Linux-gated tmux integration
cases were not run. These are author-recorded results, not a reproduced full
canonical gate or full desktop acceptance in this examination.

## Technical assessment

### PTY fix

The current implementation hardcodes Linux `TIOCSWINSZ = 0x5414` and imports
variadic `ioctl` as a fixed-signature managed call. Moving that call into C lets
the platform compiler supply both the platform request constant and calling
convention. Apple's documentation describes the special stack argument rules
for variadic calls on Apple arm64, supporting the diagnosis in the MacBook note.
[Apple ARM64 calling conventions](https://developer.apple.com/documentation/xcode/writing-arm64-code-for-apple-platforms).

Keep the existing small native shim. Retain the fixed argument-vector launch,
no managed code after fork, bounded terminal dimensions, and app-owned process
group cleanup. Rename the internal implementation and tests to `UnixPseudoTerminal`
in a separate mechanical unit, updating DI and references; the `IPseudoTerminal`
interface and terminal protocol remain unchanged. Explicitly verify `dup`,
`waitpid`, EOF/error handling, signals, and shutdown on both kernels.

The incoming child-observed resize test is stronger evidence than checking only
that ResizeAsync returns successfully. Extend its checks to repeated resize,
maximum supported dimensions, explicit EOF failure, and bounded teardown. Parse
`stty size` as two integers so whitespace differences do not cause false failures.

### Gaps beyond the incoming patch

| Area | Current evidence | Incorporation requirement |
| --- | --- | --- |
| Private server storage | `Inventory.cs:96,134,138` and `WorkspaceRecoveryControl.cs:53,56,124,155` condition private Unix file handling on Linux only | Apply equivalent owner-only creation/validation on macOS; extend permission tests and review data-protection key storage |
| tmux executable | `Options.cs:8` and server appsettings default to `/usr/bin/tmux`; integration tests hardcode it | Preserve Linux defaults; supply an explicit validated absolute path in macOS configuration and tests, including Homebrew installations |
| Native build products | Library path is assigned from `IntermediateOutputPath` in the project body; MacBook note reports output in project root; `.gitignore` only lists `.so` there | Resolve paths at the correct MSBuild phase; separate configuration/TFM/RID outputs; verify transitive copying into server/test/publish directories; ignore `.dylib` fallback output |
| Cross-target builds | Incoming MSBuild selects compiler flags by build-host OS only | Build native server artifacts on matching OS/architecture; reject unsupported server cross-publish combinations clearly; do not break existing desktop cross-publish workflow |
| Deployment | Setup script explicitly requires Linux; existing server services use systemd/Docker | Add documented native macOS server publish and per-user launchd configuration with explicit working directory, tmux path, private state and loopback bind |
| Workspace recovery | Script uses GNU `stat -c`, `base64 --wrap=0`, `readlink -f`, `flock`, associative arrays and namerefs; service templates are Linux-specific | Port utilities/locking through narrow tested adapters, declare required Bash version, and add macOS supervision while retaining the same snapshot/request format |
| Canonical checks | `verify.sh` runs GNU-dependent shell tests and Docker Compose checks unconditionally; six real PTY/tmux cases are opt-in/Linux-gated | Add explicit platform-aware verification with common tests on both systems and a required Linux deployment job; enable isolated native tmux lifecycle tests on macOS |
| Desktop evidence | Existing macOS package support; no incoming desktop source changes | Validate actual Mac launch, Dock icon, modifiers, copy/paste, resize/fullscreen, reconnection, and detach against both server OSes |

Do not assume enabling the PTY platform guard is sufficient for a secure supported
server. Do not copy the MacBook tree over the current checkout: that would discard
newer clipboard behavior and mix generated binaries with source.

## Scope

In scope:

- Shared Linux/macOS PTY implementation and native build/publish integration.
- Production-equivalent private storage and existing authenticated API behavior.
- Native macOS server build, configuration, startup/stop instructions and launchd
  template; preserve Linux Compose/systemd profiles.
- macOS workspace snapshot/explicit-restore support with the existing closed
  resume-command policy, private state and single-daemon locking contract.
- Existing Photino desktop source builds and physical acceptance on both OSes,
  including preservation of the latest clipboard behavior.
- Portable verification entry point, platform-specific suites, source-build and
  operator documentation, approved contract updates and review evidence.

Out of scope:

- Windows, Intel macOS, Linux ARM support claims, new installers, signing,
  notarization, binary publication, or new dependency/platform minimum promises.
- Docker-based control of a macOS host's native tmux socket. Proposed macOS server
  deployment is native; Linux containers retain their existing deployment path.
- Shell auto-attach configuration, automatic restore at boot, arbitrary command
  execution APIs, remote-host control, server launch by the desktop client.
- Live host changes, service installation/start, deployment, CI execution on
  external systems, merge or push as part of approving this proposal alone.

## Support and compatibility contract

| Component | Linux x64 | Apple Silicon macOS |
| --- | --- | --- |
| Server | Existing native/systemd and Linux Compose | Native source-published server; foreground and per-user launchd |
| Desktop | Existing Photino executable and launcher | Existing Photino `.app`; actual launch and Dock acceptance required |
| Workspace recovery | Existing opt-in daemon | Opt-in portable daemon, supervised by launchd; explicit restore only |
| Client/server pairings | Linux desktop → Linux and Mac servers | Mac desktop → Linux and Mac servers |
| Mobile | Existing PWA → Linux server | Same PWA → Mac server |

Preserve HTTP/WebSocket endpoints, authentication, CSRF, origin/Host controls,
rate limits, same-origin desktop hosting, server-profile schema, snapshot version,
terminal size limits, detach semantics, and Linux deployment defaults. No user-data
migration is planned. Existing native clients must continue to connect to updated
servers; no capability/protocol version change unless implementation proves a need
and that contract change is approved separately.

Use a same-user Mac server and tmux process with private state outside the publish
directory. Make the tmux absolute path explicit instead of relying on a GUI/service
PATH. Retain production authentication and loopback HTTPS-proxy setup. Validate
Tailscale Serve and DNS on the actual host before claiming remote reachability.
Native health/readiness and launchd restart behavior must be tested; the Linux
`/proc` Docker watchdog remains specific to the Linux container profile.

## Expected Files Touched

- The four files in the captured patch; optional renamed Unix PTY filenames;
  server DI registration and corresponding infrastructure tests.
- Infrastructure private storage classes; server storage/startup tests and
  platform-aware test helpers; tmux integration fixtures.
- `.gitignore`, server/native build configuration, `scripts/verify.sh`, focused
  shell suites and native server build script; CI configuration if adopted.
- `scripts/tmux-workspace-recovery.sh`, proposed `deploy/launchd/` templates and
  sanitized configuration examples, proposed `docs/macos-server.md`.
- `SPEC.md`, `DECISIONS.md`, `ROADMAP/COMMIT-PLAN.md`, `.tempo/VERIFY.md`, README,
  desktop acceptance docs, a C023 living goal and final review record.
- Desktop source only where physical acceptance identifies a scoped portability
  defect; do not rewrite the existing client merely to rename its platform support.

## Acceptance Criteria

- [ ] AC1: Clean source builds on Linux x64 and macOS arm64 generate the correct
  native library without committed binaries, source-root outputs, or stale RID
  contamination; fresh published servers load the library outside the checkout.
- [ ] AC2: A real child observes initial, repeated and maximum bounded PTY sizes
  on both OSes. Invalid sizes fail before native mutation. Input/output, natural
  exit and bounded process-group cleanup pass without leaked children/descriptors.
- [ ] AC3: Isolated real tmux tests on both OSes prove inventory, attach, Unicode
  I/O, resize, history, topology and detach; sessions and other clients survive
  app-owned client termination, abrupt disconnect and server stop.
- [ ] AC4: Production configuration and protected endpoints reject the same
  unauthorized/origin/CSRF/limit violations on both OSes; audit, keys and recovery
  files have private permissions, with unsafe storage rejected or explicitly
  diagnosed under the existing contract.
- [ ] AC5: A fresh native Mac server publish starts as the tmux owner using explicit
  configuration, reports health/readiness, and can be supervised/stopped without
  killing underlying sessions. Tailscale HTTPS works with direct backend denial.
- [ ] AC6: Recovery round-trips a disposable workspace on both OSes with identical
  versioned metadata, private atomic files and exclusive daemon operation; startup
  never restores, live tmux blocks restore, corrupt state creates nothing, and
  agent resume remains restricted to the existing fixed commands.
- [ ] AC7: Linux and Mac desktop builds launch with application identity and pass
  the existing desktop checklist against both server OSes. Clipboard copying to
  another app, guarded multiline paste, resize/fullscreen, reconnect and clean
  detach are observed; the PWA works with both servers.
- [ ] AC8: Canonical verification passes on both hosts and the required Linux
  Compose/security suites remain enforced. Platform skips are explicit, justified,
  and cannot substitute for macOS PTY/tmux or physical desktop acceptance.
- [ ] AC9: Docs and approved contracts state tested support accurately; final
  review includes exact commits, provenance, matrix evidence and rollback proof.

## Decomposition Plan

| Unit | Work and dependencies | Exit evidence | Risk |
| --- | --- | --- | --- |
| 1 | Approve C023 scope; update SPEC Linux-only server constraint and decision/roadmap; reconcile existing C022 acceptance and record one active execution goal | Approved scope, acceptance matrix, authority and one next action; do not silently complete C022 | T1 planning |
| 2 | Import native PTY and child-observed test; correct build/output paths and supported host/RID checks | Fresh Linux/Mac build and child-size test; package contains correct library | T2; depends 1 |
| 3 | Rename Unix implementation separately; generalize isolated test executable discovery and prove process lifecycle | Native lifecycle/tmux suites pass on both OSes; public interfaces unchanged | T2; depends 2 |
| 4 | Generalize private file handling and tests; audit tmux paths and native server configuration | Mac server starts privately with authenticated inventory/terminal; permission failures tested | T2; depends 3 |
| 5 | Add native server source-publish workflow, sanitized config and launchd template | Clean publish runs outside repo; supervised lifecycle/health tested on disposable Mac setup | T2; depends 4 |
| 6 | Port optional recovery script helpers/locking and add launchd supervision | AC6 round-trip, concurrency, corruption and stop tests on both hosts | T2; depends 4–5 |
| 7 | Finalize platform verification and perform four desktop/server combinations plus PWA checks | AC7–8 recorded from exact commits on actual Linux/Mac hosts | T2; depends 2–6 |
| 8 | Finish operator docs, support claims, rollback rehearsal and change review | AC1–9 evidence complete; merge-ready decision with remaining external boundaries explicit | T1 review; depends 7 |

Thin slice: after unit 4, a foreground authenticated Mac server attaches to a
disposable local tmux session and exposes correct terminal dimensions through the
existing desktop/PWA. This is a milestone, not completion of macOS support.

Verification infrastructure needed for each unit is added with that unit; unit 7
consolidates the full gate and physical matrix rather than delaying testing.

## Verification Plan

Existing canonical command: `./scripts/verify.sh` (use .NET 10 on PATH).
Do not replace the current Linux gate with a narrower macOS test command.
After approval, keep this entry point and explicitly route common versus
platform/deployment checks in `.tempo/VERIFY.md`; require Linux container checks
on a Linux runner even when a Mac has no Docker installation.

Implementation commands and evidence:

```bash
./scripts/verify.sh
npm --prefix src/TmuxMobile.Web run build
npm --prefix src/TmuxMobile.Web run build:desktop
dotnet test tests/TmuxMobile.Infrastructure.Tests --filter FullyQualifiedName~PseudoTerminal
bash scripts/build-desktop.sh linux-x64
bash scripts/build-desktop.sh osx-arm64
```

Run each native publish and PTY/tmux suite on its matching host. Add a documented
explicit opt-in invocation for generalized isolated Unix tmux tests, requiring a
resolved tmux executable and refusing a green result caused by missing prerequisites.
Inspect native artifacts with `file` and the host's dependency inspection tools;
run from a fresh publish directory with no checkout native library on its search
path. Build desktop assets from current source before publishing the server.

For macOS utilities, decide and document either a declared modern Bash/toolchain
or BSD-compatible adapters; do not silently assume GNU flags or the interactive
shell environment. Verify launchd environment, working directory, file access,
lock ownership, restart and stop behavior. Physical tests require approved access
to a Mac; SSHFS file access by itself cannot supply Mac runtime evidence.

Planning-only checks observed here: patch applicability and Linux C syntax pass.
The planning recheck `PATH="$PWD/.dotnet:$PATH" ./scripts/verify.sh` passed
with exit 0 on 2026-09-08 against unchanged application source at `d3baaae`:
158 .NET tests, 15 frontend test files, six intentionally skipped native
integration cases, plus shell/type/Compose checks. Local output is retained at
`/tmp/tmux-c023-proposal-verify.log`. These baseline results do not validate the
proposed macOS integration.

## Risks, dependencies and bounded retries

- The source note's claim of no Linux risk is too strong for a native-call change.
  Require child-observed resize and lifecycle evidence on both platforms.
- A stale or wrong-architecture native library can hide build defects. Require
  clean build/publish checks and correct output isolation, not copied `.dylib`s.
- Existing Linux-only permissions must not silently degrade on macOS. Treat
  storage and service configuration as explicit security review surfaces.
- Service supervision and recovery must not own or kill the user's tmux server.
  Exercise shutdown only on dedicated test sockets and private state directories.
- A self-contained `.app` layout is not proof of Mac launch, keyboard behavior,
  clipboard behavior or Dock acceptance. Require physical evidence.
- The MacBook source can change while mounted. Before importing, recheck HEAD and
  patch hash and resolve any new diff; retain this snapshot as review evidence.
- Dependencies: actual Mac execution environment, C compiler/SDK, .NET 10, Node,
  supported tmux and a tested shell utility set. Verify minimum versions during
  implementation; source-note versions are evidence targets, not a compatibility promise.
- Retry at most twice for an unchanged failure; document evidence and pause for
  a changed assumption/approval. If the owner reports a failed fix, perform RCA
  before another corrective attempt.

## Authority and approval

The owner approved incorporation, commits, push to the existing origin, and source
migration back to the MacBook on 2026-09-08. This authorizes the scoped T2 work
and isolated build/test validation needed for migration. C022 is paused with its
physical acceptance still outstanding. Pause for architectural expansion,
data/API breaks, unclear security/privacy, production service changes or merge
to main. Source migration does not authorize restarting the live Mac server.

Execution is tracked in `GOALS/2026-09-08--linux-macos-support.md`.

Requested from: Landon.
Approval status: Approved by Landon: "incorporate the changes and commit them and push them and then migrate our changes back over to the macos system".
Approved at: 2026-09-08 (conversation).

## Change Review and Git Plan

Create implementation branch `feat/c023-linux-macos-support` from the latest
approved integration base containing `d3baaae` (or its equivalent merged change).
Do not overwrite local changes with the older MacBook checkout. Import reviewed
source hunks from the captured patch, then make the additional integration changes
as separate commits with conventional `fix(pty)`, `feat(macos)`, `test(unix)` and
`docs(platforms)` subjects.

Required implementation trailers:

```text
Roadmap: ROADMAP/COMMIT-PLAN.md#C023
Proposal: PROPOSALS/2026-09-08--linux-macos-server-desktop.md
```

Review Boundary: merge the implementation feature branch into `main` using the
repository's approved merge process; owner approval required separately.
Planned Review Record: `REVIEWS/2026-09-08--linux-macos-server-desktop.md`.
Review must cover native ABI/output, security parity, attachment ownership,
recovery integrity, supported platform evidence, Linux regression and rollback.

## Rollback Plan

Revert C023 implementation commits in dependency order, preserving the independent
clipboard fix. Rebuild the prior source and assets, run the Linux canonical gate
and isolated lifecycle checks. There is no planned data/schema migration.

On an approved Mac rollout, retain the previous publish directory and service
configuration. Stop only the app/recovery jobs owned by this deployment, restore
prior configuration/artifacts, and confirm tmux sessions and processes survive.
Do not delete audit, keys, profiles or workspace metadata. Rolling back macOS
support may remove Mac server availability; document that limitation rather than
claiming the previous Linux-only build is a supported Mac fallback. Do not deploy
until this operator rollback procedure and Linux rollback are reviewable.
