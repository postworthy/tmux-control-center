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

## Later

- Validate the deployment on the target Linux host and iPhone.
- Consider favorites/order and read-only identities only after MVP validation.
