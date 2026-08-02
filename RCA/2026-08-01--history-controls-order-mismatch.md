# RCA: History Controls Are at the End of the Bottom Bar

Date: 2026-08-01
Severity: Low
Related Proposal(s): `PROPOSALS/2026-08-01--tmux-backed-terminal-scrollback.md`
Related Commit(s): `55c9f70`

## Symptom

- The owner clarified that Older and Latest should be the first available
  buttons in the bottom shortcut bar.
- The deployed correction placed the adjacent pair at the end of that bar.

## Reproduction

1. Open the terminal and inspect the bottom shortcut bar at its initial scroll
   position.
2. Observe Esc as the first button.
3. Older and Latest appear only after every keyboard shortcut at the trailing
   end of the horizontally scrolling list.

## Root Cause

- Contract layer: the previous clarified criterion specified membership,
  persistence, and Older/Latest adjacency, but did not specify that the pair had
  highest ordering priority.
- Implementation layer: `55c9f70` restored the pair at its historical trailing
  position, making it less immediately available than the keyboard shortcuts.
- Verification layer: checks proved stable adjacency but did not assert the
  pair were the first two children of `.shortcut-bar`, so they correctly passed
  the incomplete ordering contract.

## Corrective Action

- Move Older and Latest to the first and second positions in `.shortcut-bar`.
- Preserve stable rendering and keep Latest disabled outside history mode.

## Preventive Controls

- Regression guard: source and production bundle must order the shortcut-bar
  labels Older, Latest, Esc, Tab at the beginning of the list.
- Process update: record “first available” as an explicit first-child ordering
  requirement for horizontally scrolling mobile controls.

## Narrower Next Action

Reorder the existing buttons without changing behavior or styling, verify,
redeploy the exact-IP test container, and request physical-iPhone confirmation.
