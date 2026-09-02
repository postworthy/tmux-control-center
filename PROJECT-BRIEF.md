# PROJECT-BRIEF

Status: APPROVED
Last updated: 2026-08-31

## Onboarding Mode

- Mode: adopt-existing
- Reason: the repository already contains a committed, runnable tmux mobile MVP.

## One-Sentence Project Goal

- Let one authenticated owner observe and safely interact with tmux sessions
  through an iPhone-first PWA or a conventional desktop terminal companion,
  with the tmuxctl server reached only through Tailscale.

## Problem Statement (In User Words)

- Opening an SSH client, connecting, typing tmux commands, and using
  keyboard-oriented navigation is cumbersome on a phone.
- On a desktop, the mobile-first web controls do not provide the tabbed,
  split-pane, keyboard-and-mouse terminal experience expected from Ghostty or
  Terminator.

## Target Users

- The primary user is the owner of a Linux tmux host using an iPhone or another
  mobile device on the same private tailnet.
- The same owner may use an Ubuntu desktop or Apple Silicon Mac to connect to
  one of their saved tmuxctl server URLs through Tailscale.

## First Version Outcomes (Must Have)

- Swipe through full-height session cards and read bounded recent output.
- Open a real PTY-backed xterm.js terminal and return to the same card.
- Authenticate in production and keep terminal data out of browser persistence.
- Recover cleanly from ordinary mobile network loss, sleep, and resume.
- Save content-free tmux workspace metadata and let the owner explicitly restore
  it from the app after a host reboot, resuming only local Codex and Claude.
- Publish as a lightweight Docker Compose service whose host port binds only to
  an explicitly configured Tailscale IP.
- Provide a self-contained .NET/Photino desktop companion with a distinct
  desktop-first xterm.js interface, real tmux attachment state, tmux-backed
  session/window/pane navigation, and source builds for Ubuntu x64 and Apple
  Silicon macOS.
- Let an installed PWA keep a bounded device-local list of labelled tmuxctl
  server origins and explicitly navigate to one server at a time without
  weakening the existing same-origin authentication boundary.

## Out of Scope (Must Not Build Yet)

- Collaborative users, or one tmuxctl server controlling multiple tmux hosts.
- A combined multi-server session dashboard, cross-origin API aggregation,
  shared login credentials, or automatic profile synchronization between
  browser origins.
- Launching, installing, or supervising the tmuxctl server from the desktop
  application.
- Debian packages, DMGs, signing, notarization, Intel macOS, app stores, or
  published binary releases for the first desktop cut.
- Arbitrary command execution, filesystem browsing, automatic boot restore,
  captured-command replay, remote process restart, or automatic restart of
  tools other than local Codex and Claude Code.
- Public ingress, hosted relays, push notifications, recording, and external AI.

## Constraints

- Platform and delivery: Linux server host, local tmux server, iPhone-first PWA,
  Docker Compose, HTTPS, and exact Tailscale-IP host binding; the desktop client
  targets Ubuntu x64 and Apple Silicon macOS and assumes the remote server and
  Tailscale route already exist.
- Security/privacy/legal: application authorization remains mandatory; terminal
  input/output is not logged or cached; the service never runs as root.
- Time/budget: keep the MVP self-hosted and operationally small.

## Inferred from Codebase (Hypotheses, Adopt-Existing Mode)

- High confidence: the committed implementation covers the requested backend,
  PWA, PTY, authentication, testing, and systemd deployment foundations.
- High confidence: Docker packaging is the remaining requested delivery delta.
- Medium confidence: the containerized tmux client must be compatible with the
  host tmux server protocol; target-host validation remains necessary.

## Confirmed Facts vs Corrected Inferences

- Confirmed by the user: the original detailed MVP request is the approved
  product contract.
- Confirmed by the user: production publishing means a lightweight Docker
  Compose deployment bound only to the Tailscale IP.
- Confirmed by the user: reboot recovery preserves tmux names and directories,
  resumes only local Codex and Claude after an explicit in-app action, and
  restores other panes as shells rather than replaying their commands.
- Confirmed by the user: the first desktop cut uses Photino and xterm.js, takes
  a tmuxctl server URL, does not launch the server, maps tabs and splits to real
  tmux state, and detaches its client when a tab or window closes.
- Confirmed by the user: the desktop session list can select, create, detach,
  and explicitly kill sessions; normal terminal `exit` behavior is not
  intercepted.
- Confirmed by the user: Apple Silicon is the initial macOS architecture and
  users build from the GitHub repository before native installer work begins.
- Confirmed by the user: the PWA should gain a full-screen server chooser opened
  from its toolbar, with device-local label/HTTPS-URL profiles and top-level
  navigation to the selected server rather than cross-origin API aggregation.
- Confirmed by the user: every selected server retains its own authentication,
  cookies, service worker, offline state, and local preferences.
- Corrected: physical iPhone validation is recommended evidence, but it is not a
  prerequisite for completing the repository-delivery goal.

## V1 Done Criteria

- [x] The committed application implements the secure observation-first mobile
  workflow and automated tests.
- [x] Canonical repository verification passes.
- [x] Docker Compose builds the application and fails closed unless an exact
  Tailscale IP, HTTPS origin, TLS material, credentials, UID/GID, and tmux socket
  are supplied.
- [x] Deployment documentation explains setup, validation, upgrade, rollback,
  and the host/container tmux compatibility constraint.
- [ ] An installed PWA can manage a bounded device-local server catalog and
  explicitly navigate between independently authenticated tmuxctl origins while
  preserving the same-origin security model.
