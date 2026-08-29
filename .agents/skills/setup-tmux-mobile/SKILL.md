---
name: setup-tmux-mobile
description: Prepare and validate a first tmux-mobile deployment from a fresh Linux clone. Use when configuring a new host, installing or diagnosing tmux, checking Docker Compose or Tailscale prerequisites, generating the ignored deployment environment and login key, matching the container tmux client to the host, proving socket compatibility, configuring Tailscale Serve, starting the long-lived container, or troubleshooting first-run reachability.
---

# Set Up Tmux Mobile

Take a fresh Linux clone through diagnosis, private configuration, tmux
compatibility, and an explicitly approved Tailscale-only deployment. Keep every
privileged, secret-display, network, and long-lived-runtime change visible to
the user.

## Guardrails

- Run from the repository root as the non-root account that owns the target tmux
  server. Do not use root-owned tmux sessions.
- Do not read, print, log, or paste `deploy/docker/.env` or
  `deploy/docker/access-key.txt`. Let Compose consume the env file directly.
- Ask before sudo/package changes, `tailscale up`, Tailscale Serve changes,
  long-lived `docker compose up`, replacement of existing config, or secret
  display. Capture existing Serve/container state before changing it.
- Never install Docker Engine/Compose or Tailscale automatically. If missing,
  stop and direct the user to the official installer linked by preflight.
- Never query or mutate the default tmux server during compatibility testing.
  Use only the repository probe, which creates a unique `tmux -L` server.
- Do not weaken the loopback-only Serve backend bind (or exact-Tailscale-IP
  direct-HTTPS bind), authentication, origin/Host checks, Secure cookies, or
  direct-backend denial to make setup pass.

## Workflow

1. Read `deploy/docker/README.md` and run:

   ```bash
   ./scripts/first-run-setup.sh preflight
   ```

2. Resolve only reported prerequisites:

   - If tmux is missing, identify the host package manager and show the exact
     proposed package command. Run it only after approval, then start or confirm
     a non-critical tmux session as the current user.
   - If Docker Engine or Compose v2 is unavailable, stop and send the user to
     `https://docs.docker.com/engine/install/`. Ask them to return after
     `docker compose version` and a non-sudo `docker info` succeed.
   - If Tailscale is missing, stop and send the user to
     `https://tailscale.com/download/linux`. If installed but disconnected,
     inspect `tailscale status`, explain `sudo tailscale up`, and run it only
     after approval.

3. Obtain the exact non-secret MagicDNS hostname from Tailscale status or ask the
   user to confirm it. Run `tailscale serve --help` before composing any Serve
   command because CLI syntax changes across releases.

4. Generate private configuration without displaying it:

   ```bash
   ./scripts/first-run-setup.sh write-env --serve-host HOSTNAME
   ```

   Pass `--https-port` or `--http-port` only when the user chooses non-default
   ports. If either output file already exists, do not inspect it; ask whether
   to preserve it or replace both, and use `--force` only after confirmation.
   Report the generated paths. Tell the user they can personally reveal the
   login key with `cat deploy/docker/access-key.txt` when the app asks for it.

5. Validate and build the host-matched image:

   ```bash
   docker compose -f compose.tailscale-serve.yaml \
     --env-file deploy/docker/.env config --quiet
   docker compose -f compose.tailscale-serve.yaml \
     --env-file deploy/docker/.env build app
   ./scripts/first-run-setup.sh probe-tmux
   ```

   Stop on any failure. The build compiles the exact host release recorded as
   `TMUX_VERSION`; the probe then checks both `tmux -V` and real client/server
   communication through a disposable named socket. Never start the long-lived
   app before this passes.

6. Show the planned long-lived and Serve commands with resolved non-secret
   addresses and ports. After explicit approval, capture current state and run:

   ```bash
   tailscale serve status
   docker compose -f compose.tailscale-serve.yaml \
     --env-file deploy/docker/.env up -d app
   ```

   Configure Tailscale Serve using the syntax confirmed by its local help,
   forwarding the chosen HTTPS port to the loopback backend port.
   Do not change tailnet grants/ACLs or firewall policy without separate scope.

   Install the repository workspace helper and its template unit as documented
   in `docs/deployment.md`, with `TMUX_WORKSPACE_STATE_DIR` matching the
   `TMUX_MOBILE_WORKSPACE_DIR` host path. Run it as the tmux owner. Starting or
   enabling this service only saves and waits; it must never restore until the
   authenticated owner presses Restore in the app.

7. Verify without exposing the key:

   - Confirm `docker compose ... ps` reports healthy and inspect bounded app logs.
   - Confirm `ss -ltn` shows the configured backend port only on `127.0.0.1`,
     never the Tailscale IP, a wildcard, or a LAN address.
   - Confirm `tailscale serve status` shows the intended HTTPS-to-backend route.
   - Request `/health/live` through the HTTPS MagicDNS origin and confirm 200.
   - Request backend `/` with the configured Host and confirm 426.
   - Have the user reveal the key themselves, sign in, and confirm authenticated
     readiness plus session inventory. Confirm the app shows snapshot status;
     do not test restore against the owner's live default tmux server.
   - Require a browser or curl check from a second tailnet device. Treat local
     MagicDNS/hairpin failure as inconclusive, not proof of remote failure.

8. Report exact evidence, the URL, the key-file path (never its value), rollback
   commands, and remaining physical-device checks. Preserve state/keys and audit
   directories during upgrades or rollback.

## Recovery

- On compatibility failure, keep the long-lived service stopped, confirm the
  env-recorded version equals host `tmux -V`, rebuild without stale cache if
  necessary, and rerun only the isolated probe.
- On Serve failure, restore the captured prior Serve mapping or turn off only
  the newly approved HTTPS-port mapping using locally confirmed CLI syntax.
- On container failure, inspect bounded logs and restore the previously tagged
  image with the same env and state mounts. Never delete data-protection keys or
  audits as routine cleanup.
