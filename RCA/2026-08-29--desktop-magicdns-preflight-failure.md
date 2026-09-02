# RCA: Desktop compatibility preflight cannot resolve local MagicDNS origin

Date: 2026-08-29
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `8741bf4`, `948bbb2`

## Symptom

- On the physical Ubuntu acceptance host, connecting the desktop client to
  `https://ubuntu-box-1.monster-ionian.ts.net:8443` returns: “The server
  compatibility check could not connect. Check the server, network, TLS, and
  Tailscale connection.”
- The expected next result for the currently deployed pre-C022 server was an
  actionable server-update/invalid-compatibility response, not a transport
  failure.

## Reproduction

1. Confirm Tailscale Serve maps the HTTPS origin on port 8443 to the tmuxctl
   loopback backend on port 8780.
2. Run `getent ahosts ubuntu-box-1.monster-ionian.ts.net`; it returns no address.
3. Run `resolvectl query ubuntu-box-1.monster-ionian.ts.net`; it reports that the
   name is not found.
4. Run `tailscale dns status`; it reports `Tailscale DNS: disabled` while the
   tailnet advertises MagicDNS suffix `monster-ionian.ts.net`.
5. Request the compatibility endpoint normally; curl cannot resolve the host.
6. Repeat with curl `--resolve` using the host's current Tailscale IP
   `100.85.13.102`; TLS succeeds and the request reaches Kestrel with HTTP 200.
   The body is the existing mobile HTML shell rather than desktop capability
   JSON, independently proving that the deployed server predates C022.

## Root Cause

- The immediate transport failure occurs before HTTP or TLS negotiation: the
  Ubuntu host has Tailscale DNS acceptance disabled, so the native .NET
  `HttpClient` cannot resolve the saved MagicDNS origin. It raises a non-TLS
  `HttpRequestException`, which `DesktopCapabilityProbe` intentionally maps to
  the observed sanitized generic connection message.
- After DNS is repaired, the current deployed image is a second, sequential
  acceptance blocker. It does not implement `/api/desktop/capabilities`; its
  SPA fallback returns mobile HTML for that path, which the native probe will
  reject as an invalid compatibility response.
- Prior verification did not detect the environment failure because capability
  unit tests use an injected HTTP handler and the old-server runtime used a
  resolvable loopback test endpoint. The acceptance handoff required Tailscale
  connectivity but did not explicitly gate local MagicDNS resolution or
  `tailscale dns status` before launching the app.
- The earlier command-line diagnosis forced hostname resolution with curl to
  isolate TLS/server behavior. That correctly proved the server path but could
  not predict the desktop client's result because the application uses normal
  operating-system DNS and has no address override.

## Corrective Action

- With explicit owner approval, enable Tailscale DNS acceptance on this Ubuntu
  host, then require both normal `getent` resolution and a normal HTTPS request
  without `--resolve` to succeed.
- With separate production/deployment approval, build and deploy the reviewed
  C022 server image while preserving the existing keys, audit, workspace, tmux
  socket, Serve mapping, and rollback image. Require the capability endpoint to
  return protocol-1 JSON before retrying the desktop client.
- Do not bypass hostname validation with an IP profile, disable TLS validation,
  weaken Host/origin checks, or add a persistent hosts-file workaround.

## Preventive Controls

- Test/Guard: add `tailscale dns status` and an operating-system hostname lookup
  to the physical desktop acceptance preflight before application launch.
- Regression test: add a native probe case for a name-resolution
  `HttpRequestException` and require a specific sanitized DNS/Tailscale message
  distinct from TLS, timeout, and generic connection failures.
- Deployment gate: require the real HTTPS capability endpoint to return JSON
  with protocol 1 and the complete required feature set before distributing or
  launching the matching desktop build.

## Corrective Action Evidence

- 2026-08-29: the owner enabled Tailscale DNS acceptance. `tailscale dns status`
  now reports enabled, and normal `getent` resolution returns the host's current
  Tailscale address without an override.
- 2026-08-29: after explicit deployment approval, the prior live image
  `sha256:f48be26d...` was preserved as
  `tmux-mobile:pre-c022-desktop-rollback-20260829`. Compose validation, the C022
  image build, and the isolated host/container tmux 3.4 socket probe passed.
- The replacement image `sha256:8873036d...` is healthy with zero restarts on
  the unchanged loopback/Serve mapping. Normal HTTPS liveness returns 200,
  direct backend application access remains denied with 426, and the capability
  endpoint returns JSON protocol 1 with `session-tabs-v1`,
  `terminal-websocket-v1`, and `tmux-topology-v1`.
- The deployed root still serves the existing mobile PWA while `/desktop/`
  serves the separate tmuxctl desktop assets. Six host tmux sessions are present
  after container replacement; the deployment did not restore or stop the host
  tmux server.
