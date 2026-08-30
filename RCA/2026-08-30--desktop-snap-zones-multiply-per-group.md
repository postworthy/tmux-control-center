# RCA: Desktop snap zones multiply for every session group

Date: 2026-08-30
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `2044254`

## Symptom

- Dragging a session tab displays five snap targets inside every existing editor
  group, so each new split adds another five targets and the interaction becomes
  progressively less understandable.
- There is no explicit way to return all open tabs to the standard single-group
  tab row after experimenting with splits.

## Reproduction

1. Open at least three session tabs and drag one to create a two-group layout.
2. Begin another tab drag and observe ten targets: left, right, top, bottom, and
   center are repeated in both groups.
3. Create another split and observe fifteen targets.
4. Search the UI for an operation that collects all tabs back into one group;
   none exists.

## Root Cause

- `DesktopWorkspace.renderGroup` renders one `.drop-guidance` element containing
  all five zones whenever any tab is being dragged. Because the recursive
  renderer calls `renderGroup` once per leaf, zone count is exactly five times
  the number of groups.
- Drag handlers and `dropTarget` are keyed by `groupId`, and
  `moveWorkspaceSession` splits relative to that group. The implementation
  therefore made group-local targeting a structural assumption instead of one
  global workspace interaction.
- The approved contract required tabs to move between groups and form nested
  layouts but did not cap visible targets or require a standard-layout reset.
  The owner's physical acceptance now supersedes that ambiguity with one global
  five-zone control.
- `desktopWorkspaceLayout.test.ts` verifies group-local nesting, center moves,
  uniqueness, and empty collapse. It has no component-level zone-count guard
  and no pure flatten operation, so successful verification could not reject
  the confusing multiplication or missing recovery path.

## Corrective Action

- Move drag-over/drop handling and the guidance overlay to the single root
  `.workspace-layout`. Render exactly five labeled targets regardless of split
  depth.
- Treat edge drops as root-relative splits: the dragged session becomes one new
  edge group and the remaining layout stays intact on the other side.
- Make the center target and an explicit **Single view** sidebar action flatten
  all open tabs into one group without closing a terminal WebSocket or changing
  tmux state. Keep the dragged/current session active.

## Preventive Controls

- Pure tests must cover root-relative edge splitting, exact unique membership,
  flatten order, active-session preservation, and reset idempotence.
- A source delivery guard must require exactly one guidance overlay in
  `DesktopWorkspace` and one canonical five-zone list.
- Physical acceptance must create multiple splits, begin another drag, observe
  exactly five labeled targets, invoke **Single view**, and confirm one tab row
  remains with all attachments preserved.

## Resolution Evidence

- `DesktopWorkspace` now renders one root `.drop-guidance` overlay from the
  canonical five-entry `WORKSPACE_DROP_ZONES` list; group leaves contain no drag
  overlay or drop handler.
- Pure layout tests prove root-relative right/bottom/left splits, stable flatten
  order, preferred active session, idempotent reset, unique membership, empty-
  source collapse, and refusal to split a lone tab into an empty companion.
- The desktop delivery guard requires exactly one overlay, use of the canonical
  list, and the visible reset action. Typecheck, the production desktop build,
  all eleven frontend suites, and canonical `./scripts/verify.sh` pass. The
  emitted bundle contains all five labels and **Single view**. Deployment and
  physical acceptance remain pending explicit approval.
