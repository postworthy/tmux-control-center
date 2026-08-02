# RCA: Pinned Latest Control Is Not Beside Older

Date: 2026-08-01
Severity: Low
Related Proposal(s): `PROPOSALS/2026-08-01--tmux-backed-terminal-scrollback.md`
Related Commit(s): `2faa017`

## Symptom

- The owner rejected the pinned trailing Latest control.
- The required layout is now explicit: Latest must always appear directly near
  Older inside the bottom shortcut bar.

## Reproduction

1. Open a terminal before entering tmux history mode and inspect the bottom bar.
2. Observe that Latest is absent.
3. Enter history mode and observe Latest appear in a separate grid column beside
   `.shortcut-bar`, rather than immediately after Older inside that bar.

The source reproduces both facts: Latest is guarded by `historyMode`, and it is a
sibling of `.shortcut-bar` instead of one of its button children.

## Root Cause

- Contract layer: “integrated into the shortcut row” was still underspecified.
  The implementation optimized for permanent viewport visibility, while the
  owner intended literal membership and adjacency within the existing bottom
  shortcut bar.
- Implementation layer: `2faa017` created a two-column toolbar and conditionally
  rendered Latest outside the scrolling button group. This satisfied the
  inferred same-row geometry but not the intended control grouping or persistent
  presence.
- Verification layer: the structural check asserted that Latest was outside
  `.shortcut-bar`; it therefore enforced the incorrect interpretation. The
  checks passed because they matched the implementation contract, not the now
  clarified owner requirement.

## Corrective Action

- Render Latest unconditionally immediately after Older inside `.shortcut-bar`.
- Keep it disabled until history mode is active, preserving the existing safety
  behavior while keeping its position stable.
- Remove the separate toolbar grid column and pinned-button styling.

## Preventive Controls

- Regression guard: source and production bundle must show the stable
  Older-then-Latest order inside `.shortcut-bar`, with no `.latest-button`,
  `.has-history`, or conditional Latest rendering.
- Process update: treat explicit spatial terms such as “near” and “in the bar”
  as DOM grouping/order requirements, not only equivalent rendered geometry.

## Narrower Next Action

Restore Latest as the persistent button immediately following Older in the
scrolling bottom bar, verify, redeploy the exact-IP test container, and request
physical-iPhone confirmation.
