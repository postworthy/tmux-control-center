# RCA: Latest History Banner Breaks the Terminal Control Layout

Date: 2026-08-01
Severity: Low
Related Proposal(s): `PROPOSALS/2026-08-01--tmux-backed-terminal-scrollback.md`
Related Commit(s): `81d6d95`

## Symptom

- The owner reported that the dedicated history row above the terminal shortcut
  bar does not look good.
- The row keeps Latest fully visible, but it introduces a second control tier
  and makes a transient history action visually dominate the terminal controls.

## Reproduction

1. Open a terminal and navigate into older tmux output.
2. Observe the full-width `history-banner` appear above the existing horizontal
   shortcut bar.
3. Compare the resulting two-row layout with the normal single-row terminal
   controls; the owner rejected the additional hierarchy on the target device.

The implementation is directly reproducible from `TerminalView.tsx` and
`styles.css`: history mode renders a separate block before `.shortcut-bar`, and
the block reserves its own 44-pixel control row plus padding.

## Root Cause

- Design-contract layer: the correction interpreted “Latest must not look
  obscured” as a visibility-only requirement. It did not preserve the existing
  single-row terminal-control composition.
- Implementation layer: moving Latest out of the horizontally scrolling region
  was correct, but placing it in a new banner changed the terminal's vertical
  hierarchy rather than pinning it alongside the existing controls.
- Verification layer: source, bundle, and health checks proved that the banner
  and button existed and were reachable. They could not establish whether the
  new composition looked integrated on the physical iPhone.

## Corrective Action

- Keep Latest outside the horizontal overflow region so it cannot be clipped.
- Pin it to the trailing edge of the existing shortcut row only while history
  mode is active; do not add a separate visible status row.
- Retain an assistive-technology status announcement without visible layout
  impact.

## Preventive Controls

- Regression guard: verify that Latest is a sibling of the scrolling shortcut
  region inside one toolbar, rather than a preceding banner or a child of the
  overflow region.
- Process update: keep physical-device visual acceptance open for transient
  mobile-control layouts even when structural and production checks pass.

## Narrower Next Action

Replace the banner with a same-row, trailing pinned Latest control, run focused
and canonical verification, redeploy the exact-IP test container, and request
owner confirmation on the target iPhone.
