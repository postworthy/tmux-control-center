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

## D011 — Use Photino and xterm.js for the first desktop companion

Date: 2026-08-29

The first tmuxctl desktop client uses a self-contained .NET 10 Photino shell and
a distinct desktop-first xterm.js interface. It connects by configured HTTPS URL
to an already-running Linux tmuxctl server over the owner's Tailscale network and
does not install or supervise the server. Tmux remains authoritative and every
open terminal is a real tmux client attachment. Physical Ubuntu acceptance
superseded the initial always-visible three-level chrome. Session tabs may be
arranged into nested client-side editor layouts through one global set of five
labeled drag-drop targets; edge drops split against the whole layout, while
center or **Single view** restores one standard tab group. The targets never
multiply with split depth, and each session remains unique and owns one terminal
attachment through layout-only changes. Tmux windows and panes remain inside
normal tmux terminal interaction rather than consuming permanent subordinate
rows. The safe topology API remains additive for a future compact, explicit
surface rather than appearing by default. Right click is likewise owned by the
terminal-rendered tmux menu; tmuxctl does not overlay a second context menu.
Closing UI detaches only the associated client; session termination remains an
explicit two-click operation whose confirmation identifies the target but does
not require retyping its name, and terminal input is never intercepted to infer
termination intent. Desktop rename reuses the existing validated, audited,
inventory-resolved session endpoint and updates labels without replacing the
live terminal attachment. Ubuntu x64 and Apple Silicon macOS source builds are the
initial targets; native installers and a native terminal renderer are deferred.
Before loading server-hosted desktop content, the native shell requests a
bounded, content-free, versioned capability document from the configured HTTPS
origin. The endpoint is anonymous and rate-limited because it exposes no host,
identity, tmux, or authentication state; the client refuses redirects and
requires the closed version-1 feature set. Missing or incompatible support
returns to the native chooser with an actionable update message.

## D012 — Share a bounded high-resolution terminal grid

Date: 2026-08-31

Desktop xterm fitting, the terminal WebSocket boundary, and the Linux PTY use
one 10–2048 column by 5–1024 row contract. The upper bound preserves complete
grids on supported 5K/6K ultrawide and typical 8K displays at the minimum font
size while keeping allocation finite. The desktop clamps a still-larger fitted
grid before resizing xterm or transmitting it, the server rejects dimensions
outside the contract explicitly, and the PTY adapter enforces the same limits
for both initial allocation and later `TIOCSWINSZ` calls.
