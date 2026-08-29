# RCA: Tailscale Backend Port Missing After Host Reboot

Date: 2026-08-23
Severity: High
Related Proposal(s): N/A
Related Commit(s): deployed image
`sha256:929d2c540f11ee88b2debfff20a996b16ea5fa8ef0fedfade43b7911a63676ee`

## Symptom

- The tmux control center is unreachable through Tailscale Serve. An HTTPS
  request with DNS bypassed reaches the Tailscale listener but returns HTTP 502.
- Docker reports the application container running and healthy with zero
  restarts, while the host has no listener on the configured backend port 8780.
- The application remains healthy inside its container and can list the host's
  tmux sessions through the mounted socket.

## Reproduction

1. Reboot the host while Docker and Tailscale are enabled at boot.
2. Allow Docker to restore the existing Compose container before Tailscale has
   connected and assigned `100.85.13.102` to `tailscale0`.
3. Observe that Docker retains the requested HostConfig mapping
   `100.85.13.102:8780 -> 5179/tcp`, but the effective
   `NetworkSettings.Ports` is empty and `docker port` reports no mapping.
4. After Tailscale becomes ready, observe that it listens on port 8443 and
   retains the Serve route to `http://100.85.13.102:8780`, but the missing
   backend listener is not reconciled. The end-to-end request returns 502.
5. Observe that the container-local health check continues to pass because it
   probes `127.0.0.1:5179` inside the container rather than the published host
   port.

## Root Cause

- The Compose deployment publishes the backend to one exact Tailscale address,
  but boot ordering only makes Docker depend on generic network readiness. It
  does not ensure that Tailscale has connected and installed that address before
  Docker restores the container.
- At this boot, Docker was started by 15:56:45 and restored the app container
  during startup. The physical network did not acquire its route until 15:57:12,
  Tailscale did not reach Running until 15:57:13, and `100.85.13.102` did not
  appear on `tailscale0` until 15:57:15. The exact requested bind address was
  therefore unavailable when the container was restored.
- Docker persisted the desired port binding in `HostConfig.PortBindings`, but
  the running container has an empty effective `NetworkSettings.Ports` map.
  Docker did not retry that publication when the Tailscale address appeared.
- The health boundary is incomplete for this failure class: the container-local
  liveness probe proves Kestrel health but cannot prove that the host-published
  port or Tailscale Serve route can reach it. Consequently both Docker and the
  in-container watchdog report healthy while users receive 502.
- This is not a recurrence of the prior .NET GC starvation failure. Host load is
  low, the container responds internally, tmux socket access works, and the
  corrected runtime image is still deployed.
- The host also has Tailscale DNS acceptance disabled, so the MagicDNS hostname
  does not resolve locally. That complicates host-side diagnostics but is not
  the cause of the remote 502 demonstrated with explicit SNI/address mapping.

## Corrective Action

- Immediate recovery: now that the Tailscale address exists, force-recreate only
  the Compose app service so Docker establishes the requested host publication.
  Preserve the host tmux server, its socket, and its sessions.
- Durable correction: publish the Serve backend only on host loopback and point
  Tailscale Serve to `http://127.0.0.1:8780`. Docker can then create or restore
  the container without the Tailscale interface or address being present. Map
  Docker's actual host gateway into the container and resolve that static alias
  as an explicit forwarded-header proxy rather than trusting a broad network.
- Add a host-side boundary probe for the effective backend listener and the
  Tailscale Serve path. An in-container watchdog cannot repair a missing host
  publication because restarting the process inside the same container does not
  recreate Docker's port mapping.

## Preventive Controls

- Test: exercise the cold-boot case where Docker starts before the Tailscale
  address, then prove that reconciliation publishes port 8780 and Serve returns
  the expected authenticated application response rather than 502.
- Guard: fail deployment readiness unless the configured bind address exists
  and Docker's effective published-port state matches the requested mapping.
- Alert: report a healthy-container/missing-host-listener mismatch separately
  from application liveness failures.
- Process: production verification must include container-local liveness,
  effective Docker port publication, direct backend reachability, and the full
  Tailscale Serve path after both deployment and host reboot.

Status: root cause confirmed; immediate service recovered. Loopback design is
implemented and undergoing build/deployment verification.
