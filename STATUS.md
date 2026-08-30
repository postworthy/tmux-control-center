# Project Status

Updated: 2026-08-29

## Current

- C022 is implemented locally on `feat/c022-desktop-photino-client` and paused
  at its owner/external completion boundary. The
  .NET 10/Photino shell connects to an already-running tmuxctl server and serves
  a desktop-specific xterm.js experience with saved URL profiles, real
  attachment-aware session tabs, authoritative tmux windows/panes, native
  pop-outs, reconnect, desktop shortcuts, guarded clipboard input, ordinary
  `exit`, and exact-name session kill. A bounded native compatibility preflight
  now rejects older/incomplete servers before loading remote UI without adding
  an authentication or origin exception. Self-contained `linux-x64` and
  `osx-arm64` source builds pass; the Linux artifact launches with no installed
  .NET runtime. Focused tests, dependency audits, isolated tmux/Photino runtime
  checks, 34 desktop tests, 51 server integration tests, and canonical
  verification pass. The owner-approved predecessor-stack fast-forward and
  local history repair resolved the merge boundary and commit trailers. The
  first physical Ubuntu connection attempt exposed disabled Tailscale DNS
  acceptance and a pre-C022 deployed server. Both causes are now corrected:
  normal MagicDNS resolution works and live image `sha256:8873036d...` serves
  the exact protocol-1 contract on the unchanged tailnet-only origin. The prior
  image is preserved as `tmux-mobile:pre-c022-desktop-rollback-20260829`; health,
  loopback binding, direct-backend denial, mobile/desktop assets, and isolated
  tmux 3.4 compatibility pass. The RCA is recorded in
  `RCA/2026-08-29--desktop-magicdns-preflight-failure.md`. Actual Apple Silicon
  launch and the remainder of owner Ubuntu acceptance remain required. Further
  Tailscale changes, merge, push, and remote CI remain owner-controlled.
  Physical testing then identified missing Ctrl+mouse-wheel text zoom. The
  owner-approved local correction now provides desktop-only 8–32px bounded
  zoom, routes unmodified wheel input to tmux history, refits xterm/tmux
  dimensions, and passes focused tests, the production desktop build, and
  canonical verification.
  Continued Ubuntu acceptance also found local-xterm wheel history, initial and
  fullscreen fit failures, excessive permanent topology rows, and no sidebar
  collapse. The owner explicitly revised the first-cut desktop contract to one
  session-tab row plus a collapsible icon rail. The RCA-backed implementation
  now uses bounded/coalesced history requests, settled-layout refitting, one
  permanent session row, and the narrow icon rail. Typecheck, the production
  desktop build, all nine frontend suites, 27 core tests, 34 desktop-shell tests,
  26 infrastructure tests (5 opt-in skips), 51 server integration tests, and the
  canonical gate pass. The owner then explicitly authorized deployment. Image
  `sha256:d6dadb4f...` passed the isolated tmux 3.4 probe and is healthy with
  zero restarts on the unchanged loopback/Serve boundary; HTTPS liveness,
  protocol-1 capabilities, corrected desktop asset `index-DcoKvsuW.js`, and
  direct-backend 426 denial pass. Rollback image `sha256:8873036...` is tagged
  `tmux-mobile:pre-desktop-layout-rollback-20260829`. The self-contained Ubuntu
  launcher is rebuilt; focused physical Ubuntu interaction remains with the owner.

- C017 is deployed and published on `main`. The owner approved a
  live main-screen name filter and create-then-open workflow. Because creation
  narrows the prior no-process-launch boundary, it is scoped as T2: the request
  accepts only a validated name and tmux receives a fixed separated argument
  vector with no caller-controlled command, path, environment, socket, or
  options. The protected endpoint returns an opaque ID, refreshes shared
  inventory, maps duplicates to 409 and bounded tmux failures to 503, and audits
  all outcomes. Frontend filtering preserves recency order and create success
  enters the terminal directly. The initial review found that tmux silently
  rewrites periods and colons; create-specific validation now rejects both
  before launch and supported punctuation is preserved exactly. Fresh canonical
  verification and the changed review pass. Verified image `sha256:80f290...`
  is now healthy on the unchanged Tailscale Serve app after an isolated tmux 3.4
  compatibility probe; HTTPS liveness/root/readiness pass, direct backend remains
  denied, and the prior C015 image is preserved for rollback. Feature commit
  `416ac6b` and review checkpoint `3cf406e` are published on GitHub `main`
  through merge `b7b8e5b`. Physical testing remains a follow-up.

- C016 is active on `feat/c016-first-run-install-skill`. The approved scope adds
  a repository-local first-run setup skill, deterministic ignored environment
  generation, host-matched container tmux, and a disposable compatibility gate
  before any long-lived Compose start. Docker and Tailscale installation remain
  user-controlled prerequisites; privileged package, Tailscale Serve, and live
  deployment changes require explicit approval. The first host-matched image
  build passed, but its real isolated compatibility probe exposed a missing
  split runtime library and a failure-path cleanup-scope defect. The gate kept
  the long-lived app unchanged; RCA was recorded before correction. The final
  image now reports host-matched tmux 3.4 with complete linkage, the real
  isolated socket query and cleanup pass, focused/skill/canonical checks pass,
  and Change Review reports ready with explicit fresh-host/live follow-ups.
  Merge and push remain owner-controlled.

- C015 is active on `fix/c015-app-scroll-keyboard-focus` after the owner reported that every App Scroll swipe and
  application-mode Older/Latest opens the iPhone keyboard. Both paths explicitly
  call xterm focus, which focuses its hidden textarea and summons iOS keyboard
  input. Approved correction removes focus only from wheel interactions and
  includes redeployment to the existing test app. The two focus calls are now
  removed locally; frontend tests, typecheck, and full focus-caller inspection
  pass. Canonical verification and clean production image `sha256:2bc9c5...`
  also pass. C015 is now live and healthy; HTTPS, readiness, exact-IP binding,
  current asset identity, compatibility across 13 tmux sessions, and
  direct-backend denial checks pass. C014 is preserved as
  `tmux-mobile:pre-c015-focus-rollback`. Canonical verification, commit-trailer
  inspection, diff checks, and a full-history Gitleaks scan pass. The C012-C015
  stack is approved for the owner-authorized merge and push with physical C015
  acceptance retained as an explicit follow-up. See
  `RCA/2026-08-05--application-scroll-opens-keyboard.md`.

- C014 is deployed pending physical acceptance. The owner reported repeated terminal disconnects during C012 App Scroll. Live
  logs confirm three `Terminal input rate limit exceeded` closures: a 72-event
  velocity gesture was amplified into 72 WebSocket input messages, exceeding the
  server's intentional 64-message burst bucket. Approved corrective scope now
  coalesces one gesture into one bounded input send without weakening the
  limiter. Focused regression, typecheck, canonical verification, production
  image `sha256:6ac99e...`, docs, and review inspection pass; the correction is
  now live and healthy in the existing tailnet test environment. HTTPS,
  readiness, exact-IP binding, current asset identity, compatibility across 13
  tmux sessions, and direct-backend denial checks pass; no post-restart
  rate-limit warning is present before physical testing. C013 is preserved as
  `tmux-mobile:pre-c014-scroll-rollback`. See
  `RCA/2026-08-05--application-scroll-input-burst-disconnect.md`.

- C013 is locally implemented on `feat/c013-session-recency`: opening a terminal from the
  main deck will promote that session to the first tile on return using a safe,
  device-local opaque-ID MRU order. Backend inventory order and tmux state remain
  unchanged. Pure ordering/persistence tests, frontend typecheck, canonical
  verification, production image build, and review inspection pass. Verified
  image `sha256:e1e79f...` is now healthy in the existing tailnet test
  environment; HTTPS, readiness, exact-IP binding, current asset identity, tmux
  compatibility, and direct-backend denial checks pass. The prior velocity image
  is preserved as `tmux-mobile:pre-c013-recency-rollback`; owner acceptance
  remains pending.

- C012 is deployed for owner testing from `feat/c012-opt-in-tui-scroll`. The
  thin slice keeps
  tmux-backed swipes as the default and adds a default-off, non-persistent App
  Scroll toggle for mouse-aware TUIs. Image `tmux-mobile:c012-opt-in-tui-scroll`
  is healthy in the existing tailnet Serve environment; HTTPS liveness/root,
  readiness, served-bundle identity, tmux compatibility, and direct-backend 426
  checks pass. `tmux-mobile:pre-c012-rollback` preserves the prior live image.
  Frontend unit/type checks, the .NET 10
  container build, four isolated Linux PTY/tmux tests, negotiated xterm encoding
  probe, and canonical verification pass. Physical-iPhone acceptance and review
  completion remain before any merge or push.
- The first C012 iPhone check did not show App Scroll. RCA establishes that the
  deployed image has the new bundle, but the unchanged service worker cannot
  prompt an already-open PWA to update and stale old hashed bundles remain
  servable. Corrective work is paused pending a force-close/reopen diagnostic;
  see `RCA/2026-08-04--deployed-terminal-control-not-visible.md`. Force-closing
  and reopening made the button visible, confirming the diagnosis. The owner
  then requested proportional swipe movement and application-mode routing for
  Older/Latest. That local correction now passes focused tests, production image
  build, and canonical verification. The confirmed release defect is also
  corrected locally: the worker is release-stamped and the runtime image no
  longer contains stale hashed bundles. The revised image was not deployed; the
  owner has now requested a further velocity multiplier so fast flicks move
  materially farther than deliberate drags. That refinement now passes focused
  tests, canonical verification, and production image inspection and is live as
  image `sha256:035f7f...`. HTTPS, readiness, exact-IP binding, current asset and
  worker identity, tmux compatibility, and direct-backend denial checks pass.
  The prior three-tick image is preserved as
  `tmux-mobile:pre-c012-velocity-rollback`; physical acceptance is pending.

- C011 is implemented and actively deployed from
  `security/c011-dotnet10-and-hardening` at
  `https://ubuntu-box-1.monster-ionian.ts.net:8443/`.
- The live container is healthy on .NET/ASP.NET Core 10.0.10, remains bound only
  to `100.85.13.102:8780`, accepts the unchanged temporary access key through
  the HTTPS origin, and returns 426 for direct backend application HTTP.
- Canonical verification passes (24 core, 12 infrastructure plus 3 separately
  exercised Linux tests, 33 server integration, and frontend checks); release
  publish, image build, Compose renders, and NuGet/npm vulnerability audits
  pass. `tmux-mobile:pre-c011-rollback` retains the pre-rollout image.
- C011 is completed with a ready-with-follow-ups Change Review; merge and push
  remain explicitly owner-controlled.
- The owner reported successful iPhone testing of the actively deployed PWA on
  2026-08-02. C009 physical-device acceptance and its Review Record are complete.

- The application MVP is committed at `26c8e4d`.
- Tempo governance is committed at `f6d0990`.
- The Tailscale-only Compose deployment is committed at `5f9b717`.
- Authenticated HTTP smoke mode is committed at `2cef73c` on
  `feat/c004-tailnet-http-test`.
- Canonical verification, a production image build, a disposable HTTPS
  container startup, dependency audits, and the isolated real-tmux lifecycle
  test pass.
- The previous temporary instance was healthy and bound only to
  `100.85.13.102:8780`; C011 supersedes its runtime/security posture while
  preserving the exact mapping and application state.
- The blank-page failure is resolved by `f27414a` and documented in
  `RCA/2026-07-31--blank-anonymous-app-shell.md`.
- C004 review is ready with the explicit TLS follow-up; merge/push remain
  owner-controlled.
- C008 touch-scrollback was deployed, but its original physical-iPhone validation failed:
  xterm-local scrolling does not navigate authoritative tmux pane history.
- RCA is recorded in `RCA/2026-08-01--xterm-local-scrollback-noop.md`; C009 is
  completed with owner-confirmed device evidence.
- tmux-backed scrolling now works on the target iPhone, but the owner rejected
  the dedicated Latest banner. A narrower same-row layout correction is active
  under `RCA/2026-08-01--latest-banner-layout-miss.md`.
- The subsequent pinned same-row control was also rejected: Latest must always
  be a member of the bottom shortcut bar immediately after Older. The clarified
  correction is recorded in `RCA/2026-08-01--latest-pinned-layout-mismatch.md`.
- The owner further clarified that the Older/Latest pair must lead the shortcut
  list, recorded in `RCA/2026-08-01--history-controls-order-mismatch.md`.
- The empty public GitHub repository is configured as SSH `origin`. Initial
  publication completed with a parentless sanitized `main`; Gitleaks and
  canonical verification pass, and no pre-sanitization ref was pushed. Evidence
  is in `REVIEWS/2026-08-02--initial-github-publication.md`.

## Next

- Owner reloads/applies the C017 PWA update and physically tests live filtering
  plus creating and immediately entering a new session.

- Owner reloads/applies C015, dismisses the keyboard, and verifies both App Scroll
  directions and application-mode Older/Latest keep it closed and connected.
- C013 physical acceptance remains pending: confirm an opened terminal becomes
  tile 1 on return and remains first after reload.
- C012 physical testing remains pending: deliberate drags versus fast flicks,
  plus App Scroll Older/Latest, in Claude Code and mitmproxy.

## Known Limitations

- A containerized tmux client can require the same compatible tmux protocol
  version as the host server.
- The current access key is intentionally temporary and unchanged by C011.
- Tailnet policy changes remain human-controlled; direct-backend application
  rejection is active as defense in depth.
