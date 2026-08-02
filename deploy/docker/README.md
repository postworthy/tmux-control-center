# Docker Compose deployment

This deployment runs one non-root application container and publishes its HTTPS
port only on the host's explicit Tailscale IPv4 address. Docker bridge
networking keeps the container isolated; the `0.0.0.0` listener in
`compose.yaml` exists only inside that container namespace.

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
cookies while leaving the exact-IP port available to the local Serve proxy:

```bash
docker compose -f compose.tailscale-serve.yaml --env-file deploy/docker/.env up -d --build
```

The current temporary profile still permits an eight-character test key. Remove
that override and rotate to a strong random key after validation.

## Prepare

Run these commands as the same Linux account that owns the target tmux server:

```bash
cp deploy/docker/.env.example deploy/docker/.env
id -u
id -g
tailscale ip -4
tailscale cert
```

Edit `deploy/docker/.env` with those values, the full MagicDNS hostname shown by
`tailscale cert`, and a random application access key. If HTTPS certificates are
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
install -d -m 0700 deploy/docker/state/keys deploy/docker/state/audit
```

`TMUX_SOCKET_DIR` normally equals `/tmp/tmux-$(id -u)`. If using `tmux -L
name`, also set `TMUX_SOCKET_NAME=name`. Never mount an unrelated `/tmp`
directory or the Docker socket.

## Validate and start

```bash
docker compose --env-file deploy/docker/.env config --quiet
docker compose --env-file deploy/docker/.env build
docker compose --env-file deploy/docker/.env up -d
docker compose --env-file deploy/docker/.env ps
docker compose --env-file deploy/docker/.env logs --tail=100 app
```

Open `TMUX_MOBILE_ORIGIN` from a tailnet device. The mapping is fail-closed:
Compose refuses to render if `TAILSCALE_IP` or another security-critical value
is absent. Confirm on the host that the port is not listening on a LAN or
wildcard address:

```bash
ss -ltn
curl --fail --cacert deploy/docker/secrets/tls.crt \
  "https://${TMUX_MOBILE_HOST}/health/live"
```

Readiness can report unhealthy if the host tmux server is not running. Before
using critical sessions, confirm `/health/ready` and open a disposable tmux
session. The image's tmux client must be protocol-compatible with the host tmux
server; rebuild with a matching tmux package/version if attachment reports a
protocol mismatch.

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
