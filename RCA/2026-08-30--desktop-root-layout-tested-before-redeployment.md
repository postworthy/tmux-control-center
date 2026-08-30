# RCA: Corrected desktop root layout was tested before redeployment

Date: 2026-08-30
Severity: Low
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `2044254`, `e179c83`

## Symptom

- Owner screenshots taken after rebuilding the native Ubuntu launcher still
  show the empty root group and an opened terminal constrained to a narrow,
  content-sized column rather than the available workspace.
- This appears identical to the symptom addressed by
  `RCA/2026-08-30--desktop-root-session-group-shrinks.md`.

## Reproduction

1. Launch the rebuilt native executable from `e179c83` and connect it to the
   currently deployed tmuxctl origin.
2. Open no session and then one session; both states retain the narrow root
   group shown in the owner evidence.
3. Fetch the desktop document and referenced stylesheet from that origin.

## Root Cause

- The Photino executable does not contain the desktop React/CSS application. It
  performs compatibility preflight and then loads `/desktop/` from the selected
  tmuxctl server. Rebuilding the native executable therefore updates its icon
  and shell but cannot update editor-group CSS.
- The live container remains image `sha256:8be95175...`, created before commit
  `e179c83`. Its desktop document references `index-BgbL6qd-.css`; direct
  inspection reports `live_root_rule=absent` and shows only the nested
  `.workspace-split > .workspace-group` flex rule.
- Commit `e179c83` has a distinct source stylesheet digest and adds
  `.workspace-layout > .workspace-group, .workspace-layout > .workspace-split
  { flex: 1 1 0; }`. The goal and handoff explicitly recorded that this CSS
  awaited redeployment, so the screenshots exercise the known pre-correction
  server bundle rather than falsifying the correction.
- The process-level verification gap is that native rebuild success is easy to
  mistake for desktop frontend delivery even though the architecture separates
  native shell artifacts from server-hosted UI assets.

## Corrective Action

- Make no additional source correction from this evidence.
- After explicit owner approval, rebuild and replace only the existing Compose
  app service from `e179c83`, preserving the prior image for rollback and all
  tmux sessions.
- Before requesting physical retest, fetch the live cache-busted desktop
  document and assert that its referenced stylesheet contains the direct-root
  flex rule; then close every older tmuxctl window and launch the rebuilt client.

## Preventive Controls

- Deployment gate: physical validation of a server-hosted desktop change begins
  only after the live asset itself, not merely the native executable, proves the
  expected release marker or corrected selector.
- Handoff: distinguish native-only rebuild instructions from server UI
  redeployment instructions whenever one requested change spans both artifacts.

## Resolution Evidence

- The owner explicitly approved deployment and launcher installation. Prior
  image `sha256:8be95175...` is preserved as
  `tmux-mobile:pre-root-layout-rollback-20260830`.
- Image `sha256:a2d7f313...` passed Compose validation, in-image selector
  inspection, and the isolated host/container tmux 3.4 compatibility probe
  before replacing only the app service. It is healthy with zero restarts.
- The cache-busted live HTTPS document now references `index-KHTt-Jmq.css`, and
  those served bytes contain
  `.workspace-layout>.workspace-group,.workspace-layout>.workspace-split{flex:1
  1 0}`. Protocol metadata remains 200, direct backend traffic remains 426,
  bounded logs contain no error, and all six session/window/attachment counts
  match the immediate predeployment snapshot.
- `/home/landon/.local/share/applications/tmuxctl.desktop` is installed with
  mode `0644`, executable and icon paths resolve, `StartupWMClass=Tmuxctl`
  matches the observed GTK class, desktop-file validation exits zero, and the
  installed PNG is byte-identical to the PWA 512px icon. Physical rendering and
  full-workspace acceptance remain owner checks.
