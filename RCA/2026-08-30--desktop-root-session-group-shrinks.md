# RCA: Initial desktop session group shrinks to its tab content

Date: 2026-08-30
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `2044254`

## Symptom

- After the draggable session-group rollout, the first terminal opened in the
  unsplit workspace occupies only a small content-sized area instead of filling
  the available desktop workspace.
- Session groups nested below a split receive the expected proportional sizing.

## Reproduction

1. Launch the deployed Ubuntu client from commit `2044254` and open one session
   in the initial, unsplit workspace.
2. Observe that the root `.workspace-group` is a direct child of
   `.workspace-layout` and remains sized from its tab-strip content.
3. Create a split and observe that the nested groups match the separate
   `.workspace-split > .workspace-group` flex rule.

## Root Cause

- `DesktopWorkspace` renders either a group or split as the root layout node.
  The stylesheet gives `.workspace-split` and groups nested inside a split a
  flexible size, but never gives a root `.workspace-group` a flex basis or full
  inline/block size. The initial group therefore participates as an intrinsic-
  sized child of the flex root instead of consuming the workspace.
- Evidence: `DesktopWorkspace.tsx` places `renderNode(layout)` directly below
  `.workspace-layout`; `styles.css` contains a nested-child flex rule only for
  `.workspace-split > .workspace-group, .workspace-split > .workspace-split`.
  The root group has no matching sizing rule.
- The previous verification passed because `desktopWorkspaceLayout.test.ts`
  validates only the pure layout tree. It cannot exercise computed CSS geometry
  and did not assert the separate root-node layout boundary.

## Corrective Action

- Give both possible direct children of `.workspace-layout` an explicit
  `flex: 1 1 0` basis so the initial group and a later split tree occupy the
  same full workspace.
- Preserve the existing nested split rule and terminal fit lifecycle; the
  ResizeObserver/geometry watcher will then fit xterm to the corrected host box.

## Preventive Controls

- Test/Guard: add a source-level desktop layout contract check that requires the
  root group and root split selectors to share the flexible sizing rule.
- Physical regression: opening the first session in a fresh window must fill
  the workspace before any resize or split interaction.

## Resolution Evidence

- The stylesheet contract now gives both direct root node types the same
  `flex: 1 1 0` sizing, while retaining the nested split sizing rule.
- The production desktop Vite build passes and emits corrected stylesheet
  `index-KHTt-Jmq.css`; the desktop delivery guard and all eleven frontend
  suites pass.
- Canonical `./scripts/verify.sh` passes 41 Desktop, 55 Server integration, 27
  Core, 26 Infrastructure tests plus five intentional skips, all frontend and
  shell suites, and both Compose assertions. Physical Ubuntu confirmation
  remains required.
- After explicit owner approval, corrected image `sha256:a2d7f313...` passed the
  isolated host/container tmux 3.4 probe and replaced only the Compose app
  service. It is healthy with zero restarts; the cache-busted live document now
  references `index-KHTt-Jmq.css`, whose served bytes contain the required
  direct-root flex selector. All six tmux sessions and their predeployment
  attachment counts remain unchanged.
