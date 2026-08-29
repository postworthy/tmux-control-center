# Docker Compose deployment

This deployment runs one non-root application container. Direct HTTPS profiles
publish only on the host's explicit Tailscale IPv4 address; the Tailscale Serve
profile publishes its HTTP backend only on host loopback. Docker bridge
networking keeps the container isolated; the `0.0.0.0` listener inside the
container namespace is not a wildcard host listener.

For temporary pre-TLS validation, `compose.http-test.yaml` retains API-key
authentication while explicitly permitting non-Secure cookies. It is an unsafe
test mode, must bind only to the Tailscale IP, and must be replaced by the HTTPS
deployment after validation.

Start or stop that temporary mode with:

```bash
docker compose -f compose.http-test.yaml --env-file deploy/docker/.env up -d --build
docker compose -f compose.http-test.yaml --env-file deploy/docker/.env down
```

The access key remains in the ignored, permission-restricted
`deploy/docker/.env`. HTTP mode still requires login; it does not use the
Development authentication bypass. This temporary Compose file explicitly
allows a short test key; `compose.yaml` does not, and production HTTPS retains
the normal minimum key length.

When Tailscale Serve terminates HTTPS, use `compose.tailscale-serve.yaml` and set
`TMUX_MOBILE_SERVE_HOST` plus `TMUX_MOBILE_SERVE_ORIGIN` to the exact hostname
and HTTPS origin printed by `tailscale serve`. This profile restores Secure
cookies while leaving a loopback-only port available to the local Serve proxy:

```bash
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env up -d --build
```

The current temporary profile still permits an eight-character test key. Remove
that override and rotate to a strong random key after validation.

The Serve profile uses Docker's default bridge, maps
`host.docker.internal` to its actual gateway, and resolves that static host
entry as an explicit trusted proxy at startup. Requests that do not become
HTTPS after that trust check receive
`426 Upgrade Required`; only inexpensive
`/health/live` and loopback `/health/ready` remain on HTTP. Thus opening the
backend IP/port is not an alternate application URL.

The image disables hard per-CPU server-GC thread affinity so a contended host
CPU cannot indefinitely stop the managed runtime while other assigned CPUs are
available. Its Compose health check also maintains bounded counters in the
container tmpfs, resetting when the direct app process identity changes. After
twelve failed startup checks or six failed checks after the first success, it
terminates only the validated direct `dotnet` child;
`restart: unless-stopped` then recreates application availability without
stopping the host tmux server or its sessions. Invalid watchdog thresholds or
ambiguous process ownership fail closed without signaling another process.

## Prepare

### Guided first run

From a fresh clone, invoke the repository-local `$setup-tmux-mobile` skill with
a compatible coding agent. Its deterministic helper performs non-mutating
preflight and private configuration generation:

```bash
./scripts/first-run-setup.sh preflight
./scripts/first-run-setup.sh write-env --serve-host HOST.example.ts.net
```

The second command records the current numeric UID/GID, exact Tailscale IPv4
address, host tmux release, socket location, origins, ports, and a generated
32-byte random login key. It atomically writes ignored mode-`0600`
`deploy/docker/.env` and `deploy/docker/access-key.txt`, never prints the key,
and refuses to replace existing files unless `--force` is explicitly chosen.
The operator can personally reveal the key at login with:

```bash
cat deploy/docker/access-key.txt
```

After building, require the isolated compatibility gate before starting the
long-lived service:

```bash
docker compose -f compose.tailscale-serve.yaml \
  --env-file deploy/docker/.env build app
./scripts/first-run-setup.sh probe-tmux
```

The probe creates a unique disposable `tmux -L` server, checks the image reports
the pinned host version and can query that mounted socket, then destroys only
the disposable server. It never addresses the default socket. A failed probe
must leave the long-lived service stopped.

The skill stops with official guidance rather than installing Docker/Compose or
Tailscale. It may propose a host-package tmux installation, Tailscale login or
Serve mapping, and Compose start, but each privileged/network/live mutation
requires explicit approval.

### Manual preparation

Run these commands as the same Linux account that owns the target tmux server:

```bash
cp deploy/docker/.env.example deploy/docker/.env
id -u
id -g
tailscale ip -4
tailscale cert
```

Edit `deploy/docker/.env` with those values, the full MagicDNS hostname shown by
`tailscale cert`, the exact release token from `tmux -V`, and a random
application access key. If HTTPS certificates are
enabled for the tailnet, write the certificate files to the configured paths:

```bash
install -d -m 0700 deploy/docker/secrets
tailscale cert \
  --cert-file=deploy/docker/secrets/tls.crt \
  --key-file=deploy/docker/secrets/tls.key \
  tmux-host.example-tailnet.ts.net
chmod 0600 deploy/docker/secrets/tls.key
```

File-based Tailscale certificates expire and require renewal. Replace the files
and recreate the container before expiry.

Create protected state directories owned by the tmux user:

```bash
install -d -m 0700 deploy/docker/state/keys deploy/docker/state/audit deploy/docker/state/workspace
```

`TMUX_MOBILE_WORKSPACE_DIR` is mounted into the app only as a fixed request and
status bridge. The separately installed host recovery service must use the same
host directory. It saves automatically but never restores at boot; after a
reboot, sign in and use **Restore saved workspace** while no tmux sessions are
running.

`TMUX_SOCKET_DIR` normally equals `/tmp/tmux-$(id -u)`. If using `tmux -L
name`, also set `TMUX_SOCKET_NAME=name`. Never mount an unrelated `/tmp`
directory or the Docker socket.

If an audit file already exists, it must be owned by the service user and mode
`0600`; startup deliberately rejects a group/other-readable file:

```bash
chmod 0600 deploy/docker/state/audit/audit.jsonl
```

## Validate and start

```bash
docker compose --env-file deploy/docker/.env config --quiet
docker compose --env-file deploy/docker/.env build
docker compose --env-file deploy/docker/.env up -d
docker compose --env-file deploy/docker/.env ps
docker compose --env-file deploy/docker/.env logs --tail=100 app
```

Open `TMUX_MOBILE_ORIGIN` from a tailnet device. Confirm on the host that the
Serve backend is listening only on `127.0.0.1`, never a LAN, Tailscale, or
wildcard address:

```bash
ss -ltn
curl --fail --cacert deploy/docker/secrets/tls.crt \
  "https://${TMUX_MOBILE_HOST}/health/live"
```

For the Serve profile, also verify the browser URL and direct-backend denial:

```bash
curl --fail "${TMUX_MOBILE_SERVE_ORIGIN}/health/live"
curl -o /dev/null -w '%{http_code}\n' \
  -H "Host: ${TMUX_MOBILE_SERVE_HOST}" \
  "http://127.0.0.1:${TMUX_MOBILE_HTTP_PORT:-8780}/"
# expected: 426
```

Readiness can report unhealthy if the host tmux server is not running. Before
using critical sessions, confirm `/health/ready` and open a disposable tmux
session. The image compiles the official upstream tmux release selected by
`TMUX_VERSION`. It must match the host release token and pass
`./scripts/first-run-setup.sh probe-tmux`; a version comparison alone is not
sufficient because the gate also proves real client/server communication over
the mounted socket.

## Upgrade and rollback

Build a distinct `TMUX_MOBILE_IMAGE_TAG`, run canonical verification, and then
recreate the service. Preserve the state and TLS directories:

```bash
docker compose --env-file deploy/docker/.env build
docker compose --env-file deploy/docker/.env up -d
```

For rollback, restore the previous image tag in the environment file and run
`docker compose up -d` again. Do not delete `state/keys`; doing so invalidates
all authentication cookies. Inspect `docker compose logs app` and
`state/audit/audit.jsonl` after either operation.

Before rollout, tag the currently running image with a rollback name. Rollback
must preserve the tmux socket and state mounts; replacing the web container does
not stop the tmux server. Recheck HTTPS root, authenticated status, liveness,
readiness, listener addresses, and logs after either direction.
