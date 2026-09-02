# RCA: Desktop resolver bypasses active Tailscale MagicDNS routing

Date: 2026-08-31
Severity: Medium
Related Proposal(s): `PROPOSALS/2026-08-29--photino-desktop-companion.md`
Related Commit(s): `7785051`, `85c2379`

## Symptom

- Immediately after the two-click kill-confirmation deployment, the physical
  Ubuntu desktop client reports: “The server compatibility check could not
  connect. Check the server, network, TLS, and Tailscale connection.”
- The live server container remains healthy and the same HTTPS origin succeeds
  when its hostname is explicitly mapped to the known tailnet address.

## Reproduction

1. Confirm `tailscale dns status` reports Tailscale DNS enabled and MagicDNS
   enabled for `monster-ionian.ts.net`.
2. Run `resolvectl query ubuntu-box-1.monster-ionian.ts.net`; systemd-resolved
   returns `100.85.13.102` through `tailscale0`.
3. Run `getent hosts ubuntu-box-1.monster-ionian.ts.net`; it returns no address.
4. Request the normal HTTPS capability URL with curl; curl reports `Could not
   resolve host`, matching the native .NET probe's transport-level error path.
5. Repeat with curl `--resolve` using `100.85.13.102`; hostname/TLS validation
   succeeds and `/api/desktop/capabilities` returns protocol-1 JSON.
6. Inspect resolver integration: `/etc/resolv.conf` correctly symlinks to
   `/run/systemd/resolve/stub-resolv.conf`, but that target contains only
   `nameserver 1.1.1.1` and `nameserver 8.8.8.8`. It was modified at
   2026-08-30 23:30:18 and is owned by `nobody:nogroup`. `nsswitch.conf` uses
   `dns` and the optional `libnss-resolve` module is not installed.

## Root Cause

- Tailscale correctly registered `100.100.100.100` and the MagicDNS routing
  domains with the active systemd-resolved service. `resolvectl`, which talks to
  that service directly, therefore succeeds.
- Ordinary glibc clients, including curl and .NET `HttpClient`, follow the
  `hosts: ... dns ...` NSS path and read `/etc/resolv.conf`. The file advertised
  as systemd-resolved's local stub was overwritten with public upstream DNS
  addresses, so those clients bypass `127.0.0.53` and never reach Tailscale's
  split-DNS route. Public DNS cannot resolve the private MagicDNS hostname.
- The server, certificate, protocol, Compose rollout, and Photino capability
  contract are not failing: forced-address HTTPS reaches the deployed server
  with valid hostname verification, the container is healthy at zero restarts,
  and direct backend application traffic remains denied with 426.
- The deployment verification exposed this discrepancy before handoff, but
  accepted forced-address HTTPS as sufficient application evidence and deferred
  the normal-resolver mismatch to physical acceptance. That was insufficient
  because the native desktop probe has no address override and depends on the
  same ordinary resolver path as curl.

## Corrective Action

- With explicit owner approval for a host DNS service change, restart
  `systemd-resolved`. The existing `/etc/resolv.conf` symlink is already correct;
  service restart should regenerate its managed stub target with
  `nameserver 127.0.0.53` without changing Tailscale policy or application
  configuration.
- Before retrying tmuxctl, require all of these to pass without overrides:
  `/etc/resolv.conf` contains the local stub, `getent ahostsv4` returns the
  tailnet address, curl reaches the HTTPS capability endpoint, and the native
  desktop compatibility probe advances to the server-hosted UI.
- If restart does not regenerate the stub, stop before editing resolver files
  manually and investigate which process rewrote the runtime-managed file.
- Do not use an IP profile, disable TLS validation, weaken Host/origin checks,
  or add a persistent hosts-file bypass.

## Preventive Controls

- Deployment gate: a successful forced-address request proves server/TLS state
  only; it must not satisfy desktop reachability when normal `getent` or curl
  resolution fails.
- Acceptance: require `resolvectl` and ordinary NSS resolution to agree before
  launching the native client.
- Operations: treat unexpected non-stub contents in
  `/run/systemd/resolve/stub-resolv.conf` as host configuration drift and pause
  before application changes.
