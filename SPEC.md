# SPEC - Tmux Mobile Control Center

Version: 1.7
Last updated: 2026-08-29
Status: Approved

## Product Objective

- Provide an observation-first, self-hosted mobile control surface and a
  desktop terminal companion for local tmux sessions without exposing a
  shell-control service to the public internet.

## Users and Core Workflows

- One Linux/tmux owner authenticates from an installed iPhone PWA over Tailscale.
- The owner swipes between stable full-screen session cards, reads bounded
  previews, invokes safe quick actions, and opens a real terminal only when
  intervention is necessary.
- The owner exits terminal mode without losing the selected session and can
  reconnect after ordinary mobile network changes.
- On Ubuntu or Apple Silicon macOS, the owner selects a saved tmuxctl HTTPS URL,
  authenticates through the existing application boundary, and uses a
  keyboard-and-mouse desktop terminal with session tabs and tmux-backed windows
  and splits.

## Functional Requirements

- FR1: enumerate sessions and panes through machine-oriented tmux formats without
  invoking a shell.
- FR2: expose opaque identifiers and validate every session/pane target against
  current tmux inventory.
- FR3: provide bounded, sanitized previews and conservative rule-based status.
- FR4: expose typed REST endpoints for inventory, capture, rename, text, keys,
  and interrupt; never expose arbitrary command execution.
- FR5: use one shared background inventory poller and retain REST fallback.
- FR6: bridge xterm.js to a real Linux PTY running a tmux attach client.
- FR7: clean up PTY children without killing the underlying tmux session.
- FR8: provide iPhone-first scroll-snapped cards, visible navigation, safe-area
  handling, accessible controls, and a mobile terminal shortcut bar.
- FR9: provide an offline shell and reconnect states without caching API or
  terminal data.
- FR10: require production authentication, CSRF protection, authorization
  policies, origin/Host controls, rate limits, security headers, and auditing.
- FR11: publish through Docker Compose without a wildcard host bind. The
  Tailscale Serve profile binds its HTTP backend only to host loopback so Docker
  startup is independent of Tailscale address readiness; direct-HTTPS profiles
  use an explicitly configured Tailscale-IP bind.
- FR12: run the container as the same numeric non-root UID/GID that owns the
  target tmux server and mount only the required tmux socket directory and state.
- FR13: keep tmux-backed terminal history navigation as the default and provide
  an explicit device-local, session-scoped terminal control that translates
  vertical swipe distance and velocity plus the Older/Latest controls into
  bounded, directionally equivalent mouse-wheel input for foreground TUIs;
  once selected, the mode persists for only that session across terminal exits,
  reconnects, and app reloads until explicitly disabled.
- FR14: order main session tiles by device-local in-app terminal recency so the
  session most recently opened from a tile returns to the top of the deck while
  untouched sessions retain stable server order.
- FR15: coalesce all negotiated application-wheel reports produced by one touch
  gesture into bounded terminal input serialization so velocity scrolling cannot
  exhaust the per-connection WebSocket message bucket.
- FR16: keep App Scroll gestures, its toggle, and application-mode Older/Latest
  focus-neutral so wheel-only interactions never summon the software keyboard.
- FR17: ship a repository-local first-run skill that diagnoses Linux host
  prerequisites, safely prepares ignored deployment configuration and a login
  secret, matches the image tmux client to the host tmux release, proves socket
  compatibility with an isolated session, and guides an explicitly approved
  Tailscale Serve deployment through reachability checks.
- FR18: provide a main-screen session-name search that filters the current
  session deck after every edit without submission, and allow the authenticated
  owner to create one detached tmux session from a validated name through a
  typed, audited, rate-limited API before opening its terminal immediately.
- FR19: make sessions with no attached tmux clients visually distinct on the
  main deck and state clearly that no terminal is attached while the tmux
  session itself remains running.
- FR20: provide an All/Detached main-deck filter composed with live name search,
  and let the authenticated owner terminate one explicitly confirmed tmux
  session through a typed, audited, rate-limited endpoint that resolves only an
  opaque current-inventory target into a fixed `kill-session` invocation.
- FR21: keep HTTP liveness, startup, shutdown, and supervised recovery responsive
  during bounded host CPU contention, tmux subprocess delay, and terminal churn;
  isolate blocking subprocess/PTY lifecycle work from Kestrel worker capacity,
  expose tmux degradation through readiness and stale inventory, and contain
  unrecoverable non-progress without terminating underlying tmux sessions.
- FR22: periodically preserve content-free host tmux workspace metadata and,
  only after an explicit authenticated in-app action, reconstruct session/window
  names, panes, layouts, directories, and active selections when tmux is empty;
  automatically invoke fixed directory-scoped resume commands only for local
  Codex and Claude Code panes, while every other program restores as a shell.
- FR23: provide a self-contained .NET 10/Photino desktop companion with a
  desktop-only xterm.js presentation for Ubuntu x64 and Apple Silicon macOS;
  it connects to an already-running tmuxctl server by user-supplied HTTPS URL
  and never installs, launches, or supervises that server.
- FR24: store multiple device-local server profiles containing only a label and
  validated URL, reuse the server's authentication, authorization, CSRF,
  origin, and rate-limit protections, and never persist terminal content or a
  plaintext login secret in application settings.
- FR25: represent one tmux session as one top-level desktop session tab, expose
  tmux windows as subordinate tabs, and expose real tmux panes as splits so the
  same topology persists and remains visible to mobile and other tmux clients.
- FR26: enumerate, select, create, detach from, and explicitly kill sessions,
  and create/select/close tmux windows and panes only through typed,
  inventory-resolved, authorized, audited, and rate-limited operations rather
  than arbitrary tmux or shell commands.
- FR27: every open desktop terminal is a real tmux client attachment; closing a
  tab or desktop window detaches only the clients owned by that UI scope,
  unexpected process or network loss clears stale attachments within a bounded
  heartbeat interval, and other attached clients or the tmux session survive.
- FR28: preserve ordinary terminal semantics: input such as `exit` is never
  intercepted, so it closes only the shell/pane/window that tmux would normally
  close; terminating an entire session remains a distinct named-confirmation
  action in the session list.
- FR29: make the desktop interface behave like a conventional Linux terminal
  with keyboard-driven tabs, splits, focus, selection, copy/paste, context
  menus, resizing, and reconnection states, without rendering the PWA's mobile
  cards, swipe navigation, touch shortcut bar, or oversized mobile controls.
- FR30: provide documented repository-source build and test commands that
  produce self-contained `linux-x64` and `osx-arm64` desktop outputs without a
  preinstalled .NET runtime; native installers and published binaries are not
  required in this cut.

## Constraints

- The tmuxctl server supports Linux and one local tmux host; desktop clients
  support Ubuntu x64 and Apple Silicon macOS and may save profiles for multiple
  independently deployed servers.
- The browser never executes shell commands and tmux remains authoritative.
- Tailscale is defense in depth, not a replacement for application security.
- Docker deployment requires host/container tmux protocol compatibility.
- Secrets, TLS private keys, audit data, data-protection keys, and captured
  terminal content must remain outside the image and repository.

## Risk Model

- T1 for repository-local governance and packaging.
- T2 for the desktop architecture, remote authentication bridge, new typed tmux
  topology operations, attachment lifecycle, and cross-platform compatibility.
- T2/T3 boundaries include changing host permissions, tailnet policy, secrets,
  production deployment, or public/network exposure and require explicit
  approval before execution.

## Acceptance Criteria

- [x] AC1: unit and integration tests cover parsing, validation, authorization,
  limits, WebSocket access, and PTY lifecycle boundaries.
- [x] AC2: the PWA and backend build into one ASP.NET Core application.
- [x] AC3: production configuration fails when authentication or origin controls
  are absent or unsafe.
- [x] AC4: `./scripts/verify.sh` passes from the repository root.
- [x] AC5: `docker compose config` passes with the safe example configuration
  and rejects a missing required Tailscale IP.
- [x] AC6: the production image builds successfully.
- [x] AC7: docs provide exact Compose setup and verification steps without
  suggesting `0.0.0.0` as a host bind.
- [ ] AC8: terminal swipes and Older/Latest continue to navigate tmux history by
  default, while an explicitly enabled application-scroll mode routes bounded,
  distance- and velocity-scaled wheel input plus directionally equivalent
  Older/Latest wheel input to mouse-aware foreground programs and persists only
  for that session across exit, reconnect, and reload until explicitly disabled.
- [ ] AC9: opening a session terminal promotes that session to the first main
  tile on return, persists the device-local recency order safely across refresh
  and reload, and keeps deck navigation and the session rail consistent.
- [ ] AC10: a maximum application-scroll gesture preserves every ordered xterm
  wheel report while producing one bounded terminal input message and no
  rate-limit disconnect.
- [ ] AC11: dismissing the iPhone keyboard and using App Scroll in either
  direction or through Older/Latest leaves the keyboard closed while scrolling
  remains functional and connected.
- [ ] AC12: a fresh-clone operator can use the repository-local setup skill to
  reach a validated Compose configuration without exposing its generated login
  secret, and the long-lived deployment is not started until an isolated
  host/container tmux compatibility probe passes.
- [ ] AC13: editing the main-screen search immediately filters session tiles by
  name without changing inventory or stored recency, clearing it restores the
  consistently ordered deck, and empty results are explicit.
- [ ] AC14: submitting a valid, non-conflicting session name creates exactly one
  detached tmux session through the authenticated typed API and opens its
  terminal, while invalid, duplicate, unauthorized, rate-limited, or failed
  requests do not create or open a session and return actionable feedback.
- [ ] AC15: a session with zero attached tmux clients has a prominent,
  non-error visual treatment and a clear "No terminal attached" explanation;
  attached sessions retain their existing presentation.
- [ ] AC16: All/Detached filtering preserves coherent deck ordering and states;
  killing requires a named confirmation and one protected request terminates
  only its inventory-resolved target, refreshes inventory, audits the outcome,
  and handles authorization, CSRF, rate-limit, missing-target, and tmux failures.
- [ ] AC17: under an isolated constrained-resource workload, subprocess and PTY
  lifecycle resources remain bounded, liveness stays responsive while tmux is
  blocked, readiness degrades and recovers explicitly, graceful stop is
  zombie-free, and induced unrecoverable non-progress exits for supervised
  restart without ending the underlying tmux session.
- [ ] AC18: an owner-only atomic snapshot round-trips an isolated multi-session
  tmux workspace after server loss without storing terminal content, argv,
  environment, credentials, or remote targets; boot remains idle, one protected
  in-app request initiates restore, only classified Codex and Claude panes launch
  fixed resume commands, live sessions block restore, corrupt state creates
  nothing, and stopping the recovery daemon leaves tmux alive.
- [ ] AC19: from a clean source checkout, documented commands produce
  self-contained Ubuntu x64 and Apple Silicon macOS desktop outputs; the Ubuntu
  app launches without a machine-wide .NET runtime, while an actual macOS build
  or approved macOS CI runner proves the Apple Silicon output launches.
- [ ] AC20: the desktop app validates and saves multiple label/URL profiles,
  connects through Tailscale to an already-running server, completes the
  existing protected login flow without storing a plaintext login secret in
  settings, and gives actionable offline, authentication, and TLS errors.
- [ ] AC21: opening a listed session creates a real tmux client and changes
  inventory attachment state; closing its tab, closing the desktop window, or
  losing the client unexpectedly removes only its owned attachment within the
  bounded timeout while the session and any other client remain alive.
- [ ] AC22: one session tab renders tmux window tabs and real tmux pane splits;
  create, select, resize, and close actions update authoritative tmux topology
  and are visible after reconnect and from the mobile client.
- [ ] AC23: session creation and named-confirmation termination operate on only
  the inventory-resolved target, while closing a tab merely detaches and typed
  `exit` retains normal tmux pane/window/session semantics.
- [ ] AC24: keyboard navigation, focus, selection, copy/paste, context menu,
  terminal resize, reconnect, and independent desktop windows pass automated
  interaction checks and owner acceptance on Ubuntu without exposing the
  mobile card deck or touch shortcut controls.
- [ ] AC25: all new server operations reject unauthorized, cross-origin,
  rate-limited, stale, malformed, or caller-command-bearing requests; focused
  security/integration tests and canonical `./scripts/verify.sh` pass, docs
  match behavior, rollback is proven, and a Tempo Change Review is ready.

## Canonical Verification

- Command: `./scripts/verify.sh`

## Safety and Capability Boundaries

- No arbitrary shell, filesystem, restart, bulk/automatic tmux cleanup, or
  remote-host capability enters the MVP. Process launch is limited to creating
  one detached tmux session with a validated name and tmux's configured default
  command. Destruction is limited to one explicitly confirmed current session
  resolved from an opaque ID into a fixed `kill-session` argument vector. The
  client cannot supply a command, arguments, environment, working directory, or
  raw tmux target.
- The host recovery service is the only automatic process-restart exception. It
  cannot receive caller-controlled commands or persist/replay argv: one protected
  no-arguments app request maps the closed pane class set `codex`, `claude`, and
  `shell` to two fixed resume commands or no command. It never restores at boot.
- Desktop topology actions are a closed set of fixed tmux operations. Callers
  may provide validated names, opaque inventory targets, and bounded layout
  dimensions where required, but never commands, arguments, environments,
  filesystem paths, or raw tmux target expressions.
- No agent may deploy, modify Tailscale policy, handle production secrets, push,
  or publish without separate explicit approval.
- Compose interpolation must stop before deployment when security-critical
  inputs are missing.

## Compatibility and Migration

- HTTP and WebSocket API contracts remain unchanged by container packaging.
- Existing systemd/nginx deployment remains supported.
- The mobile PWA, its routes, and existing API clients remain supported while
  the desktop client and additive typed topology operations are introduced.
- The Docker image contains its own tmux client; deployment must confirm it can
  communicate with the host tmux server before using critical sessions.

## Non-Goals

- One server controlling multiple hosts, orchestration platforms, native iOS
  packaging, public ingress, collaboration, notifications, history indexing,
  and external LLMs.
- Automatic boot restore, SSH reconnection, remote-agent restore, arbitrary tool
  adapters, process-memory checkpointing, terminal-content snapshots, and exact
  agent-ID association.
- Desktop-managed server installation/startup, a bundled tmux server, native
  terminal rendering, Electron, Linux distributions beyond the initial Ubuntu
  target, Intel macOS, Windows, `.deb`/`.dmg` installers, signing,
  notarization, app-store delivery, and published release binaries.
