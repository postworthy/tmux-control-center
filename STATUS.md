# Project Status

Updated: 2026-08-02

## Current

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
  preserved as paused follow-up evidence.
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

- Obtain owner approval before merging or pushing the completed C011 branch.
- C009 device acceptance remains preserved as a paused physical-device follow-up.

## Known Limitations

- A containerized tmux client can require the same compatible tmux protocol
  version as the host server.
- The current access key is intentionally temporary and unchanged by C011.
- Physical iPhone installed-mode, sleep/wake, and network-change acceptance
  remains owner/device validation; automated and live HTTP checks cannot prove it.
- Tailnet policy changes remain human-controlled; direct-backend application
  rejection is active as defense in depth.
