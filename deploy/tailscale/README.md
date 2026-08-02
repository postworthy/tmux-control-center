# Tailscale deployment notes

For systemd, keep ASP.NET Core on `127.0.0.1`. The Compose Serve profile instead
publishes only on the exact `TAILSCALE_IP` required by this project. Both shapes
terminate browser HTTPS with Tailscale Serve or the supplied reverse proxy.
Restrict the HTTPS destination to the owner identity/device through tailnet
grants or ACLs. Do not grant ordinary clients the backend port; the application
also rejects unforwarded HTTP application traffic on that port.

The public browser origin must exactly match `Security__AllowedOrigins__0`, and the hostname must appear in top-level `AllowedHosts`. Tailscale access is an additional boundary; leave application authentication enabled.

See [the full deployment guide](../../docs/deployment.md).
