# RCA: Deployed Terminal Control Is Not Visible in an Existing PWA Runtime

Date: 2026-08-04
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-04--opt-in-terminal-tui-scrolling.md`
Related Commit(s): `d3f6edb`, `a86fa5e`, `b9dc938`

## Symptom

- After C012 was deployed and server-side checks found `App Scroll` in the live
  terminal bundle, the owner reported that the button was not visible in the
  installed iPhone app.
- The intended control is the third item in the horizontal shortcut bar, after
  Older and Latest, so normal toolbar overflow cannot account for its absence.

## Reproduction

1. Compare the preserved pre-C012 image with the deployed C012 image. Their
   `service-worker.js` files have the identical SHA-256 digest
   `4fba5613b78c118dd14d2fc0247f06c493aa6b6330a94c9a9440e2746f6789ce`.
2. Observe that the service worker is the only release-update detector. The app
   displays `Update ready` only after `updatefound` produces a waiting worker.
   An identical service-worker script produces no new worker and no prompt.
3. Observe that an already-running pre-C012 app retains
   `index-DZIeX__m.js`, which lazy-loads `TerminalView-DmF5LBU9.js`.
4. Inspect the deployed C012 image. It contains both of those old hashed files
   in addition to the new `index-COxA0Inn.js` and
   `TerminalView-DqM62OrI.js`. The old terminal chunk does not contain
   `App Scroll`; the new terminal chunk does.
5. Therefore an app left running across deployment continues executing its old
   main bundle and can still load the old terminal screen successfully from the
   new server, without an update prompt or a missing-chunk error.

The live network root currently references the new main bundle, and the new
terminal chunk contains `App Scroll`. This rules out the wrong container image
and supports stale client runtime as the explanation. The owner subsequently
force-closed and reopened the installed PWA; App Scroll then appeared, confirming
the stale client runtime as the immediate cause.

## Root Cause

- Update-contract layer: releases do not change the service-worker script or
  cache version (`tmux-mobile-shell-v1`). Consequently the installed app's
  `updatefound`/waiting-worker UI cannot announce ordinary application bundle
  changes, and an already-running standalone PWA is not reloaded.
- Build-output layer: Vite writes outside its project root and the image build
  overlays generated output onto the tracked `wwwroot` directory without first
  removing obsolete hashed assets. The new image therefore continues serving
  the complete old JavaScript graph, allowing the stale runtime to operate
  normally and conceal the release mismatch.
- Verification layer: deployment verification fetched the current network root
  and searched the current terminal chunk. It did not exercise an installed PWA
  held open across the old-to-new image transition, nor assert that the image
  contains only the asset graph referenced by its current HTML.

## Corrective Action

- Immediate diagnostic: fully close the installed iPhone PWA and reopen it so
  a fresh navigation loads the current root and current hashed bundles. Confirm
  that App Scroll then appears third in the shortcut bar.
- Release fix: make every web release produce a detectable service-worker
  update while retaining the explicit Apply flow, and generate `wwwroot` from
  a clean output directory so obsolete hashed assets cannot ship.
- Re-run the original physical-device scenario after a release transition; do
  not treat a direct bundle-content request as sufficient PWA-update evidence.

## Preventive Controls

- Regression test: build two distinct web revisions, verify their service-worker
  update identity differs, and verify the second image contains no hashed assets
  reachable only from the first revision.
- Deployment guard: validate the root-referenced asset graph and reject stale
  unreferenced application bundles in the runtime image.
- Acceptance boundary: include an already-open installed PWA in release tests,
  apply the visible update prompt, and then confirm the new terminal control.

## Narrower Next Action

The force-close/reopen diagnostic confirmed the immediate cause. Implement the
two release-boundary controls above before another C012 deployment.
