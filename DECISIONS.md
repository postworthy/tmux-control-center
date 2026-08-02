# Durable Decisions

## D001 — Preserve the existing application stack

Date: 2026-07-30

The application remains ASP.NET Core 8 plus React/TypeScript and npm. Tempo is
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
