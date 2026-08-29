# Roadmap and Commit Plan

## C001 — Adopt Tempo governance

Status: completed

- Install portable repo-local skills and kernel.
- Record approved project contracts and durable decisions.
- Add canonical verification and an active deployment goal.
- Planned commit: `chore(governance): adopt Tempo workflow`

## C002 — Add Tailscale-only Compose deployment

Status: completed

- Add a multi-stage production image.
- Add fail-closed Compose configuration and safe example environment.
- Document build, TLS, tmux socket, validation, upgrade, and rollback.
- Planned commit: `feat(deploy): add Tailscale-only Compose deployment`

## C003 — Review the adoption boundary

Status: completed

- Run canonical verification and focused container checks.
- Audit scope, security, generated files, rollback, and history.
- Record the Change Review.
- Planned commit: `docs(review): record Tempo adoption review`

## C004 — Authenticated tailnet HTTP smoke deployment

Status: completed

- Add an explicit test-only HTTP cookie mode that retains API-key authentication.
- Add a separate Compose definition that publishes only on a required Tailscale
  IP and never changes the HTTPS production definition.
- Start the service on the current host, verify authentication, inventory, and
  exact listener binding, then provide the URL.
- Planned commit: `feat(deploy): add authenticated tailnet HTTP smoke mode`

## C005 — Temporary test password

Status: completed

- Permit a short API key only behind both explicit HTTP-test safety switches.
- Set the ignored live test key to `[redacted test key]`, recreate the exact-IP container,
  and verify authentication and listener scope.
- Planned commit: `feat(auth): allow explicit short HTTP test key`

## C006 — Tailscale Serve HTTPS cutover

Status: completed

- Configure the application for the exact Tailscale Serve HTTPS host and origin.
- Restore Secure cookies while retaining the explicitly approved temporary
  `[redacted test key]` key behind a bounded test override.
- Replace the HTTP test container, then verify HTTPS login, inventory,
  WebSockets, health, and the exact backend bind.
- Planned commit: `feat(deploy): add Tailscale Serve HTTPS profile`

## C007 — Mobile terminal clipboard paste

Status: completed

- Add an accessible Paste control to the terminal shortcut bar.
- Use xterm.js paste semantics, provide a Safari/manual fallback, and confirm
  multiline or large text before sending.
- Bound serialized WebSocket input chunks below the server message limit without
  persisting or logging clipboard text.
- Build, verify, review, and update the current Tailscale Serve test container.
- Planned commit: `feat(terminal): add guarded clipboard paste`

## C008 — Terminal touch scrollback

Status: completed

- Add natural one-finger vertical drag scrolling inside the xterm viewport.
- Preserve taps/keyboard input and ignore horizontal or multi-touch gestures.
- Add visible Older and Latest controls as non-gesture alternatives.
- Build, verify, review, and update the current Tailscale Serve test container.
- Planned commit: `feat(terminal): add touch scrollback navigation`

## C009 — tmux-backed terminal scrollback correction

Status: completed

- Replace ineffective xterm-local history navigation with bounded tmux copy-mode
  controls.
- Add regression coverage across the frontend/WebSocket/tmux-command boundary.
- Rebuild, verify, review, and require owner confirmation on a physical iPhone.
- Planned commit: `fix(terminal): navigate tmux-backed scrollback`
- Owner confirmed successful physical-iPhone testing on 2026-08-02.

## C010 — Sanitized initial public publication

Status: completed

- Configure the public GitHub repository as SSH `origin`.
- Redact the live test key from tracked content without changing the ignored
  deployment environment.
- Publish a Gitleaks-clean parentless `main` so pre-sanitization commits are not
  reachable remotely.
- Preserve old local history only in an ignored permission-restricted bundle and
  remove its branch refs from the working repository.
- Planned commit: `docs(review): record sanitized initial publication`

## C011 — Security hardening and .NET 10 LTS migration

Status: completed

- Migrate and verify the complete application/container/test stack on .NET 10
  LTS before making security behavior changes.
- Resolve security-review findings 2-10 across fail-closed authentication,
  PTY cleanup, audits, rate limits, configuration, headers, health, and the
  Tailscale Serve backend boundary.
- Preserve the temporary access key, application contracts, exact Tailscale-IP
  host bind, and tmux workloads; deploy and review the verified result.
- Planned commits: staged under
  `PROPOSALS/2026-08-02--security-hardening-and-dotnet10.md`.
- Implementation, active deployment, and Change Review are complete. The
  owner-controlled merge/push boundary remains outside C011 execution.

## C012 — Opt-in terminal TUI scrolling

Status: active correction after owner physical-iPhone feedback

- Preserve tmux-backed swipe history as the default terminal behavior.
- Add an explicit application-scroll toggle that translates bounded vertical
  swipes into negotiated xterm mouse-wheel input.
- Retain accessible pressed-state feedback and cover both routing modes; C020
  supersedes the original exit/connection reset with session-scoped persistence.
- Verify ordinary tmux history and mouse-aware alternate-screen behavior before
  physical-iPhone acceptance.
- Scale application wheel ticks with swipe distance and route Older/Latest as
  wheel-up/wheel-down bursts only while application scrolling is enabled.
- Apply a bounded velocity multiplier so a fast flick moves materially farther
  than a slow drag of comparable distance.
- Planned commit: `feat(terminal): add opt-in TUI swipe scrolling`

## C013 — Session tiles ordered by in-app recency

Status: active

- Record an opaque device-local MRU list when a session terminal is opened from
  the main deck.
- Put the opened session first on return while retaining stable server order for
  sessions without an in-app recency record.
- Apply the derived order consistently to tiles, navigation, selection, and the
  session rail; tolerate malformed and stale local state.
- Planned commit: `feat(sessions): order tiles by terminal recency`

## C014 — Coalesce application-scroll input bursts

Status: active

- Buffer the negotiated xterm wheel reports emitted by one synthetic gesture and
  send them once through the existing bounded terminal-input serializer.
- Preserve the 1x–4x velocity model, 72-event cap, byte order, modifier behavior,
  server limiter, and all default scrolling behavior.
- Add a maximum-gesture regression proving one WebSocket message instead of a
  limiter-exhausting burst.
- Planned commit: `fix(terminal): coalesce application scroll input`

## C015 — Keep application scrolling focus-neutral

Status: active

- Remove xterm textarea focus from wheel-only dispatch and the App Scroll toggle.
- Preserve focus for reconnect, typing shortcuts, modifiers, and paste.
- Verify swipes and application-mode Older/Latest scroll without opening the iOS
  keyboard or regressing C014 connection stability.
- Planned commit: `fix(terminal): keep app scroll focus neutral`

## C016 — First-run host setup skill and tmux compatibility

Status: completed locally; ready with approval-gated follow-ups

- Add a repository-local skill for first-clone Linux setup and deployment.
- Diagnose tmux, Docker Compose, and Tailscale; install only tmux with explicit
  approval and direct users to official Docker/Tailscale installation guidance.
- Generate ignored, permission-restricted Compose configuration and a strong
  login key without printing or reading the secret back to the agent.
- Build the image with the host tmux release and require an isolated socket
  compatibility probe before starting the long-lived service.
- Guide explicitly approved Tailscale Serve configuration and verify exact-IP
  binding, container health, and tailnet reachability.
- Planned commit: `feat(setup): add first-run deployment skill`

## C017 — Live session search and guarded session creation

Status: deployed for physical acceptance; local commit/merge/push pending

- Add a main-screen search field that filters the recency-ordered session deck
  by name after every character without submitting or changing inventory.
- Add a validated, CSRF-protected, authorized, rate-limited, and audited session
  creation endpoint that accepts only a name and returns the opaque created ID.
- Create a session from the main screen and open its terminal immediately while
  preserving recency, navigation, reconnect, and error behavior.
- Planned commit: `feat(sessions): add search and guarded creation`

## C019 — Runtime starvation resilience

Status: active; recurrence RCA and containment thin slice implemented locally

- Reproduce the production starvation with a constrained disposable container
  and isolated tmux socket before changing runtime architecture.
- Bound and isolate subprocess output, PTY reads, and child waits so they cannot
  consume the worker capacity required by Kestrel.
- Decouple HTTP liveness from initial and ongoing tmux inventory, report
  readiness/staleness explicitly, and contain unrecoverable non-progress through
  session-preserving supervised restart.
- Prove bounded descriptors, children, threads, startup, liveness, shutdown,
  and recovery in a 60-minute constrained soak before an approval-gated canary.
- Planned commits: staged under
  `PROPOSALS/2026-08-15--runtime-starvation-resilience.md`.

## C020 — Persist App Scroll per session

Status: active

- Keep new and never-enabled sessions default-off.
- Persist an explicit App Scroll choice on this device by opaque session ID
  across terminal exit, reconnect, and reload until explicitly disabled.
- Bound and defensively parse local preference state so one session's mode never
  changes another session.
- Preserve wheel routing, coalescing, focus neutrality, and backend contracts.
- Planned commit: `feat(terminal): persist app scroll per session`

## Later

- Validate the deployment on the target Linux host and iPhone.
- Consider favorites/order and read-only identities only after MVP validation.
