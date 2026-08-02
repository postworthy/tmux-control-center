# Tailscale deployment notes

Keep the ASP.NET Core listener on `127.0.0.1` and terminate HTTPS with Tailscale Serve or the supplied local reverse proxy. Restrict the HTTPS destination to the owner identity/device through tailnet grants or ACLs.

The public browser origin must exactly match `Security__AllowedOrigins__0`, and the hostname must appear in top-level `AllowedHosts`. Tailscale access is an additional boundary; leave application authentication enabled.

See [the full deployment guide](../../docs/deployment.md).
