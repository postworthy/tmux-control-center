# SPEC - Tmux Mobile Control Center

Version: 1.0
Last updated: 2026-07-30
Status: Approved

## Product Objective

- Provide an observation-first, self-hosted mobile control surface for local
  tmux sessions without exposing a shell-control service to the public internet.

## Users and Core Workflows

- One Linux/tmux owner authenticates from an installed iPhone PWA over Tailscale.
- The owner swipes between stable full-screen session cards, reads bounded
  previews, invokes safe quick actions, and opens a real terminal only when
  intervention is necessary.
- The owner exits terminal mode without losing the selected session and can
  reconnect after ordinary mobile network changes.

## Functional Requirements

- FR1: enumerate sessions and panes through machine-oriented tmux formats without
  invoking a shell.
- FR2: expose opaque identifiers and validate every session/pane target against
  current tmux inventory.
- FR3: provide bounded, sanitized previews and conservative rule-based status.
- FR4: expose typed REST endpoints for inventory, capture, rename, text, keys,
  and interrupt; never expose arbitrary command execution.
- FR5: use one shared background inventory poller and retain REST fallback.
- FR6: bridge xterm.js to a real Linux PTY running a tmux attach client.
- FR7: clean up PTY children without killing the underlying tmux session.
- FR8: provide iPhone-first scroll-snapped cards, visible navigation, safe-area
  handling, accessible controls, and a mobile terminal shortcut bar.
- FR9: provide an offline shell and reconnect states without caching API or
  terminal data.
- FR10: require production authentication, CSRF protection, authorization
  policies, origin/Host controls, rate limits, security headers, and auditing.
- FR11: publish through Docker Compose with HTTPS and an exact Tailscale-IP host
  bind; never publish the host port on a wildcard address.
- FR12: run the container as the same numeric non-root UID/GID that owns the
  target tmux server and mount only the required tmux socket directory and state.

## Constraints

- Linux and one local tmux host are supported in v1.
- The browser never executes shell commands and tmux remains authoritative.
- Tailscale is defense in depth, not a replacement for application security.
- Docker deployment requires host/container tmux protocol compatibility.
- Secrets, TLS private keys, audit data, data-protection keys, and captured
  terminal content must remain outside the image and repository.

## Risk Model

- T1 for repository-local governance and packaging.
- T2/T3 boundaries include changing host permissions, tailnet policy, secrets,
  production deployment, or public/network exposure and require explicit
  approval before execution.

## Acceptance Criteria

- [x] AC1: unit and integration tests cover parsing, validation, authorization,
  limits, WebSocket access, and PTY lifecycle boundaries.
- [x] AC2: the PWA and backend build into one ASP.NET Core application.
- [x] AC3: production configuration fails when authentication or origin controls
  are absent or unsafe.
- [x] AC4: `./scripts/verify.sh` passes from the repository root.
- [x] AC5: `docker compose config` passes with the safe example configuration
  and rejects a missing required Tailscale IP.
- [x] AC6: the production image builds successfully.
- [x] AC7: docs provide exact Compose setup and verification steps without
  suggesting `0.0.0.0` as a host bind.

## Canonical Verification

- Command: `./scripts/verify.sh`

## Safety and Capability Boundaries

- No arbitrary shell, filesystem, process-launch, restart, destructive tmux, or
  remote-host capability enters the MVP.
- No agent may deploy, modify Tailscale policy, handle production secrets, push,
  or publish without separate explicit approval.
- Compose interpolation must stop before deployment when security-critical
  inputs are missing.

## Compatibility and Migration

- HTTP and WebSocket API contracts remain unchanged by container packaging.
- Existing systemd/nginx deployment remains supported.
- The Docker image contains its own tmux client; deployment must confirm it can
  communicate with the host tmux server before using critical sessions.

## Non-Goals

- Multiple remote hosts, orchestration platforms, native iOS packaging, public
  ingress, collaboration, notifications, history indexing, and external LLMs.
