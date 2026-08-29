# Durable Decisions

## D001 — Preserve the existing application stack

Date: 2026-07-30

The application remains ASP.NET Core plus React/TypeScript and npm. Tempo is
adopted through its portable profile and must not replace the product toolchain.

## D002 — Keep tmux and PTY access local

Date: 2026-07-30

The service uses explicit process arguments, opaque inventory-derived targets,
and a real PTY-backed tmux client. It does not emulate a terminal with
capture/send polling and does not expose arbitrary shell execution.

## D003 — Use application authentication behind Tailscale

Date: 2026-07-30

Tailscale restricts reachability, while secure-cookie authentication,
authorization, CSRF protection, origin validation, and rate limits remain
mandatory.

## D004 — Adopt Tempo's portable repo-local skills

Date: 2026-07-30

Tempo commit `ec572a5172442ccdac502982b075c0cb95006ebd` is installed through
its official adopt-existing installer. Project contracts, goals, proposals,
roadmap, verification, and reviews are owned by this repository.

## D005 — Publish Compose only on an exact Tailscale address

Date: 2026-07-30

Docker uses bridge networking and an explicit host port mapping whose address is
required from `TAILSCALE_IP`. Kestrel may listen on the container namespace, but
Docker must publish no wildcard host address. HTTPS terminates in Kestrel using
externally mounted certificate/key files. The process runs with the numeric
UID/GID that owns the host tmux server.

## D006 — Treat tmux as authoritative for interactive history

Date: 2026-08-01

An attached tmux client owns pane history navigation through copy mode. xterm's
local browser scrollback is not an authoritative or reliable substitute,
especially when the alternate screen is active. Mobile history gestures use a
typed, bounded backend operation with fixed tmux arguments rather than arbitrary
terminal or shell commands.

## D007 — Use the public GitHub repository through SSH

Date: 2026-08-02

The canonical Git remote is `git@github.com:postworthy/tmux-control-center.git`
under the local name `origin`. SSH transport avoids embedding GitHub credentials
in repository configuration. Because the destination is public, no ref may be
pushed until a full-history secret review passes and no reachable commit names a
usable credential. Initial push scope remains an explicit owner-controlled
publication boundary.

## D008 — Use .NET 10 LTS as the supported server runtime

Date: 2026-08-02

All server, infrastructure, core, test, publish, and production-container targets
use .NET 10 LTS. Runtime migration was completed and verified as a
behavior-neutral thin slice before C011 security behavior changes. The deployed
pre-C011 image is retained locally as `tmux-mobile:pre-c011-rollback` for the
bounded rollback window.

## D009 — Separate action outcome from audit and proxy outcome

Date: 2026-08-02

An audit append reports its own success without rewriting a tmux action that
has already completed; operators receive structured sink errors and Linux
startup enforces owner-only storage. For externally terminated HTTPS, trusted
forwarded headers run first and the application rejects remaining HTTP
application traffic. Anonymous liveness and loopback readiness are the only
HTTP exceptions. This preserves the exact Tailscale-IP Docker bind while making
the Serve backend unusable as an ordinary browser application origin.

## D010 — Restore rebooted workspaces only after an app request

Date: 2026-08-29

Workspace recovery runs as the tmux owner on the host rather than inside the web
container. Snapshots contain names, topology, layouts, directories, selection,
and only a `codex`/`claude`/`shell` classification. Boot and service start never
restore. One authenticated, authorized, CSRF-protected, no-arguments app action
signals restoration, which proceeds only when tmux has no sessions. The two
agent classes map to fixed directory-scoped resume commands; every other prior
process, including SSH, returns as a shell. Terminal content, argv, environment,
credentials, and remote targets are never persisted or replayed.
