# Project Status

Updated: 2026-08-04

## Current

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

- Owner reloads/applies the PWA update, opens a non-first session terminal, and
  confirms it becomes tile 1 on return and remains first after reload.
- C012 physical testing remains pending: deliberate drags versus fast flicks,
  plus App Scroll Older/Latest, in Claude Code and mitmproxy.

## Known Limitations

- A containerized tmux client can require the same compatible tmux protocol
  version as the host server.
- The current access key is intentionally temporary and unchanged by C011.
- Tailnet policy changes remain human-controlled; direct-backend application
  rejection is active as defense in depth.
