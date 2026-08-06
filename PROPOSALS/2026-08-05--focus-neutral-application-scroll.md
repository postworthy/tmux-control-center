# Proposal: Focus-Neutral Application Scroll

Date: 2026-08-05
Owner: Human Partner and AI Agent
Risk Class: T1
Related RCA: `RCA/2026-08-05--application-scroll-opens-keyboard.md`
Roadmap Item: C015
Planned Branch: `fix/c015-app-scroll-keyboard-focus`
Expected Commit Count: 2

## Objective

Keep App Scroll wheel-only interactions from focusing xterm's hidden textarea so
the iPhone software keyboard remains closed during swipes and Older/Latest.

## Scope

In scope:

- Remove focus from the shared application-wheel dispatcher and App Scroll
  toggle.
- Preserve focus for connect/reconnect, typing shortcut keys, Ctrl/Alt modifiers,
  and paste completion.
- Preserve velocity, coalescing, wheel routing, default history, and recency.
- Run focused/canonical checks, production build, docs/review, and owner-approved
  redeployment to the existing tailnet test app with rollback.

Out of scope:

- Changing keyboard behavior for typing controls, disabling touch keyboard
  globally, CSS hacks, xterm forks, backend changes, merge, push, or publication.

## Acceptance Criteria

- [ ] App Scroll dispatch and toggle contain no terminal/textarea focus action.
- [ ] Reconnect, keys, modifiers, and paste retain their intentional focus paths.
- [ ] Swipes and application-mode Older/Latest preserve wheel behavior,
  coalescing, connection stability, and default routing.
- [ ] Focused/canonical tests, clean image, docs, review, deployment checks, and
  physical iPhone keyboard acceptance pass.

## Verification Plan

```bash
npm --prefix src/TmuxMobile.Web run test:unit
npm --prefix src/TmuxMobile.Web run typecheck
./scripts/verify.sh
```

Source inspection must classify every remaining `focus()` call. Production and
physical checks must retain C013/C014 markers, connection stability, and a
closed keyboard for wheel-only interactions.

## Change Review Plan

- Review Boundary: merge from `fix/c015-app-scroll-keyboard-focus` into `main`
- Review Record: `REVIEWS/2026-08-05--focus-neutral-application-scroll.md`
- Reviewer expectation: RCA traceability, exact focus diff, compatibility,
  rollback, canonical/image/deployment evidence, and owner acceptance.

## Decomposition Plan

1. Focus contract — remove only wheel/toggle focus — verify by complete caller
   inspection and typecheck — exit with typing focus preserved.
2. Compatibility — run scroll/input suites and canonical verification — exit
   with C013/C014 and limiter tests green.
3. Package/deploy — build, preserve live image, deploy, check boundaries, and
   request physical keyboard acceptance.

Thin slice: after unit 1, a wheel-only action never calls xterm focus.

## Rollback Plan

Revert C015 or restore the pre-C015 image. No data, schema, or configuration
migration is involved.

## Risks and Mitigations

- Risk: removing focus could affect typing. Mitigation: remove only two
  wheel-specific calls and enumerate every retained typing focus path.
- Risk: scrolling regresses. Mitigation: no wheel event, buffering, or routing
  code changes; rerun C012/C014 tests and physical check.

## Compatibility / Migration Notes

- Backend, WebSocket, tmux, storage, auth, and network contracts are unchanged.

## Approval

- Approval status: approved, including redeployment to the existing test app.
- Approved at: 2026-08-05 through the owner's explicit request for RCA, fix, and
  redeploy after reporting the iPhone keyboard regression.
