# RCA: Desktop WebView retained the pre-correction release

Date: 2026-08-29
Severity: High
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `99b2af7`, `8e8e6a6`

## Symptom

- After image `sha256:d6dadb4f...` was built, compatibility-probed, deployed,
  and reported healthy, the owner still observed the complete prior desktop UI:
  Ctrl+wheel did not resize text, the three-level tab chrome remained, and the
  left sidebar had no collapse control.
- The three failures move together and contradict the deployed source, which
  removes the topology rows and renders the collapse control unconditionally.

## Reproduction

1. Fetch the live `/desktop/` entry and its referenced JS/CSS. The entry points
   to `index-DcoKvsuW.js`; its SHA-256 digest exactly matches the file inside the
   running image.
2. Inspect that live bundle. It contains `sidebar-collapsed`, `Collapsed sidebar`,
   the font/history wheel paths, and fullscreen refit listeners. It contains no
   `TopologyBar`, `topology-bar`, `window-strip`, or `pane-strip` marker.
3. Inspect rollback image `sha256:8873036...`. Its desktop entry references the
   distinct old `index-Bx5LY2LT.js` and `index-BhBMc95V.css` graph.
4. Fetch the live `/desktop/` response headers. Despite the static-file callback
   intended to set `no-cache` for `index.html`, the actual default-document
   response contains no `Cache-Control` or `Pragma` header.
5. Inspect the native navigation. Every connection calls `window.Load()` with
   the same `/desktop/` URI, while Photino/WebKit persists its HTTP cache under
   `~/.cache/tmuxctl/WebKitCache` and `~/.cache/tmuxctl/CacheStorage`.

## Root Cause

- Release-boundary implementation: desktop HTML relies on a filename-based
  static-file callback for cache control, but the `/desktop/` default-document
  route observed in production does not receive that header. Hashed assets are
  safe to cache; the HTML document that selects their release graph is not.
- Native navigation: Photino loads the identical desktop URI after every
  compatibility check and supplies no release or navigation nonce. WebKit can
  therefore reuse the old document and its already-cached old hashed assets,
  presenting a coherent but obsolete UI after server replacement.
- Verification: deployment checks fetched the current HTML and current asset
  directly. They proved the server image but did not prove which release graph
  an existing persistent WebKit profile rendered. Health, capabilities, and
  source-marker inspection could not falsify stale client cache behavior.

## Corrective Action

- Set explicit `no-store, no-cache` and `Pragma: no-cache` headers for every
  desktop document/fallback request independently of static-file filename
  resolution. Keep hashed `/desktop/assets/` files outside this rule.
- Add a non-secret unique navigation token to each native `/desktop/` load while
  preserving an optional opaque session deep link. This forces a document
  request even for WebKit profiles that retained the previous response.
- Rebuild the native launcher and Docker image, repeat the isolated tmux probe,
  deploy only after focused tests and canonical verification pass, then require
  the owner to confirm all three stale-release symptoms disappear together.

## Preventive Controls

- Integration test: desktop document and fallback paths must carry explicit
  non-storable headers; hashed assets remain independently addressable.
- Native unit test: every desktop navigation URI includes a caller-supplied
  cache token and safely composes an optional session target.
- Deployment acceptance: verify a persistent pre-deployment WebKit profile, not
  only direct HTTP responses, and do not claim interaction success until the
  owner observes the new controls and gestures.

## Corrective Verification

- All 35 native desktop tests pass, including cache-token navigation and safe
  composition with an optional session target.
- Five focused server tests pass for the capability contract, three desktop
  document/fallback paths, and the hashed-asset exclusion.
- The canonical repository gate passes with 27 core, 35 native desktop, 26
  infrastructure (5 opt-in skips), 55 server integration, and all 9 frontend
  suites. Runtime rollout and owner interaction remain separate evidence.
- Corrective image `sha256:ba16379d...` passed the isolated host/container tmux
  3.4 socket probe and replaced only the Compose app service. It is healthy with
  zero restarts on the unchanged loopback/Serve boundary.
- The actual live `/desktop/?desktopLoad=verification123` response now contains
  `Cache-Control: no-store, no-cache` and `Pragma: no-cache`; HTTPS liveness is
  200 and the exact protocol-1 capability contract remains available. Image
  `sha256:d6dadb4f...` is preserved as
  `tmux-mobile:pre-webview-cache-rollback-20260829`.
- Owner observation from a newly launched corrected binary remains required;
  server/image evidence alone is not interaction acceptance.
