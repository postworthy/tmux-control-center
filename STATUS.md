# Project Status

Updated: 2026-08-01

## Current

- The application MVP is committed at `26c8e4d`.
- Tempo governance is committed at `f6d0990`.
- The Tailscale-only Compose deployment is committed at `5f9b717`.
- Authenticated HTTP smoke mode is committed at `2cef73c` on
  `feat/c004-tailnet-http-test`.
- Canonical verification, a production image build, a disposable HTTPS
  container startup, dependency audits, and the isolated real-tmux lifecycle
  test pass.
- The temporary instance is healthy and bound only to
  `100.85.13.102:8780`; root, JS, CSS, manifest, service worker, login, and real
  inventory paths now pass.
- The blank-page failure is resolved by `f27414a` and documented in
  `RCA/2026-07-31--blank-anonymous-app-shell.md`.
- C004 review is ready with the explicit TLS follow-up; merge/push remain
  owner-controlled.
- C008 touch-scrollback was deployed, but physical-iPhone validation failed:
  xterm-local scrolling does not navigate authoritative tmux pane history.
- RCA is recorded in `RCA/2026-08-01--xterm-local-scrollback-noop.md`; C009 is
  active on `fix/c009-tmux-backed-scrollback`.
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

- Have the owner fully reopen the deployed PWA and confirm Older/Latest are the
  first two bottom-bar controls on the physical iPhone.

## Known Limitations

- Physical iPhone/Tailscale behavior has not been exercised from this workspace.
- A containerized tmux client can require the same compatible tmux protocol
  version as the host server.
- The current HTTP cookie mode is intentionally temporary and does not provide
  browser-level TLS; the tailnet transport and API-key authentication remain.
- Certificate issuance and tailnet policy changes remain human-controlled.
