# PROJECT-BRIEF

Status: APPROVED
Last updated: 2026-07-30

## Onboarding Mode

- Mode: adopt-existing
- Reason: the repository already contains a committed, runnable tmux mobile MVP.

## One-Sentence Project Goal

- Let one authenticated owner observe and safely interact with local tmux
  sessions from an installable iPhone-first PWA reached only through Tailscale.

## Problem Statement (In User Words)

- Opening an SSH client, connecting, typing tmux commands, and using
  keyboard-oriented navigation is cumbersome on a phone.

## Target Users

- The primary user is the owner of a Linux tmux host using an iPhone or another
  mobile device on the same private tailnet.

## First Version Outcomes (Must Have)

- Swipe through full-height session cards and read bounded recent output.
- Open a real PTY-backed xterm.js terminal and return to the same card.
- Authenticate in production and keep terminal data out of browser persistence.
- Recover cleanly from ordinary mobile network loss, sleep, and resume.
- Publish as a lightweight Docker Compose service whose host port binds only to
  an explicitly configured Tailscale IP.

## Out of Scope (Must Not Build Yet)

- Multiple hosts or collaborative users.
- Arbitrary command execution, filesystem browsing, process restart, or
  destructive session actions.
- Public ingress, hosted relays, push notifications, recording, and external AI.

## Constraints

- Platform and delivery: Linux host, local tmux server, iPhone-first PWA,
  Docker Compose, HTTPS, and exact Tailscale-IP host binding.
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
