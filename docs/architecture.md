# Architecture

## Boundaries

```text
React PWA or server-hosted desktop React/xterm.js entry point
  ├─ REST /api/*                  bounded reads and typed actions
  ├─ WebSocket /ws/inventory      shared complete snapshots
  └─ WebSocket /ws/terminal/:id   PTY bytes plus resize/input envelopes
          │
ASP.NET Core
  ├─ authentication / authorization / CSRF / limits
  ├─ shared InventoryStore + one InventoryPollingService
  ├─ ITmuxService + ITmuxTargetResolver
  ├─ IProcessRunner
  ├─ IPseudoTerminalFactory / IPseudoTerminal
  └─ IAuditLogger
          │
local tmux server
```

## Desktop companion

`TmuxCtl.Desktop` is a self-contained .NET 10 Photino shell. Its native chooser
stores multiple user-labelled, validated tmuxctl origins in the operating
system application-data directory. The versioned JSON file is written
atomically with owner-only permissions on Unix and contains no login key or
terminal content. Selecting a profile first probes the server's content-free,
rate-limited `/api/desktop/capabilities` contract over the validated HTTPS
origin. Only a compatible response navigates the operating system WebView to
that server's `/desktop/` entry point. The server build produces a separate
desktop React bundle under `wwwroot/desktop`; the mobile PWA remains the root
entry point and does not share its card, swipe, or touch-toolbar presentation.

Serving the desktop entry point from the tmuxctl origin is an intentional
security boundary. API-key login, Strict cookies, antiforgery tokens, REST
requests, inventory WebSockets, and terminal WebSockets remain same-origin.
The desktop client does not require CORS, a relaxed SameSite policy, a bearer
token bridge, embedded credentials, or a privileged remotely callable
capability. The anonymous compatibility response is fixed, content-free
metadata; it is rate-limited, redirects are refused by the native client, and
its body is bounded before parsing.
Plain HTTP server origins are rejected by the native shell except for explicit
loopback development. Remote desktop content may ask the native shell to return
to its chooser, but profile create, update, and delete messages are accepted
only while that native chooser is displayed. A successful desktop page sends a
non-secret readiness message to cancel the native 12-second navigation
watchdog; an initially offline server or TLS failure therefore returns to the
chooser instead of trapping the user in the operating-system WebView error.
Missing, too-new, or incomplete desktop protocol support is rejected before
remote content loads and produces an explicit server/client update message.

The source build reuses the PWA's 512px artwork as the Linux Photino window
icon and generates matching Ubuntu launcher metadata whose startup class agrees
with the actual GTK window. Apple Silicon output is a standard `.app` directory
with `Info.plist` and a multi-size ICNS resource derived from the same artwork.
These are native identity and pinning assets, not an installer or signing layer.

Opening a desktop session tab connects to the existing terminal WebSocket and
therefore starts one real `tmux attach-session` client. Every open tab remains
mounted while another tab is selected, so switching visibility does not detach
it. Closing a tab or cleanly closing the window closes its socket and disposes
only its PTY attach client; abrupt loss is detected by the server's 20-second
heartbeat at the latest. The tmux session and other clients remain alive.
Inventory and terminal connections retry with exponential backoff capped at 30
seconds, and tmux inventory remains authoritative for attached state. Dragging
any session tab renders exactly one root-level overlay with five labeled zones.
Edge drops remove that session from its current group, collapse any empty leaf,
and split it against the complete remaining tree on the selected side. Center
drop and **Single view** flatten the visual-order tab sequence into one group.
These immutable layout transforms do not create tmux panes or reconnect terminal
WebSockets: every session appears once and retains one attachment. Tmux windows
and panes remain authoritative
and are controlled through ordinary tmux interaction inside the terminal; the
typed topology API remains available for a future compact, opt-in presentation
rather than consuming permanent terminal height.

Desktop rename calls the existing same-origin, antiforgery-protected session
rename endpoint with the opaque inventory ID and validated name. The inventory
stream remains authoritative for sidebar and tab labels; reconciling a changed
name updates mounted tab metadata without unmounting xterm or reconnecting its
terminal WebSocket.

The desktop xterm host captures wheel events before WebView or xterm local
scroll handling. Ctrl-modified input changes font size one point at a time
within an 8–32px bound; unmodified input is coalesced to at most four typed tmux
history operations per second. Initial activation and viewport changes use an
immediate animation-frame fit plus delayed settled-layout fits, with observers
on both the host and terminal stage and explicit window/fullscreen/visibility
triggers. Each successful dimension change is reported through the existing
terminal resize envelope. The browser, WebSocket boundary, and Linux PTY share
a finite 10–2048 column by 5–1024 row contract. That ceiling preserves complete
fitting on supported 5K/6K and typical 8K displays at the minimum desktop font
size while bounding tmux grid allocation; a still-larger proposal is clamped in
xterm before it is sent. This desktop-only fitting behavior does not enter the
mobile terminal component.
The root editor node and nested split nodes all receive an explicit flexible
basis so an unsplit first session consumes the same complete workspace as later
nested layouts before xterm performs its fit.

Workspace recovery adds a deliberately narrow side channel beside the tmux
socket. A host daemon running as the tmux owner writes an atomic metadata-only
snapshot and watches an owner-private directory. The authenticated Admin action
writes a fixed request record there; only then, and only when tmux is empty, the
host daemon reconstructs the workspace. Service startup never restores.

The snapshot contains session/window names, numeric indices, layouts, active
selections, pane working directories, and only `shell`, `codex`, or `claude` as
the process classification. It contains no output, argv, environment, SSH
destination, credential, or arbitrary command. Codex maps to `codex resume
--last`, Claude maps to `claude --continue`, and every other process maps to the
tmux default shell. Missing directories fall back to the owner's home.

`TmuxMobile.Core` contains stable models, interfaces, validation, sanitization, key encoding, inventory comparison, and status rules. It has no ASP.NET Core or process-execution dependency.

`TmuxMobile.Infrastructure` owns direct process and operating-system behavior. `ProcessRunner` always uses `ProcessStartInfo.ArgumentList`, never a shell command string, drains stdout and stderr independently, enforces cancellation/timeouts, and retains bounded output. `TmuxService` uses explicit tab-delimited tmux format strings and maps raw `$session`/`%pane` targets to stable SHA-256-derived opaque IDs.

Session creation is the sole process-launch capability outside the terminal
attach lifecycle. It accepts a normalized subset of the rename grammar,
rejecting periods and colons that tmux would silently rewrite, and uses one
fixed tmux argument shape: detached session, printed numeric raw ID,
and caller-supplied name as one separated argument. The raw ID is validated and
converted to an opaque ID before returning; duplicate-name stderr is mapped to a
bounded conflict and other tmux output is not exposed.

Single-session termination is the sole destructive tmux capability. The server
re-resolves one browser-facing opaque ID against current session IDs and invokes
the fixed separated argument vector `kill-session -t RAW_ID`. No request field
can supply a raw target, option, command, pane, window, or bulk selector.

`TmuxMobile.Server` composes options, policies, routes, shared polling,
WebSockets, health checks, security middleware, and static PWA/desktop hosting.
The frontends consume only domain JSON and never see a raw tmux target.

## Inventory and previews

One singleton background service polls metadata. Connected browsers do not create polling loops. `InventoryStore` publishes only snapshots whose domain records changed and sends the current complete snapshot to each new client. REST remains independent of the realtime channel.

`TmuxService` caches previews per active pane using the separate preview interval. Metadata can poll every three seconds without capturing every pane every time. Captures are line- and byte-bounded; ANSI/OSC/control sequences are removed before JSON serialization. React renders output as text inside `<pre>` and never uses HTML injection.

## Terminal lifecycle

The server validates the opaque session ID against current tmux state before
allocating a terminal. A tiny native boundary calls `forkpty` and executes the
child before returning to managed code:

```text
/usr/bin/tmux [-L configured-socket] attach-session -t validated-raw-target
```

The PTY master is bridged to xterm.js. Input and terminal output are never
logged. Resize uses `TIOCSWINSZ`. A connection ends on browser close,
cancellation, idle timeout, PTY exit, application shutdown, or network loss.
Disposal sends HUP/TERM and finally KILL only to the attach-client process group,
then reaps its leader; tmux's server and session remain alive. Real
isolated-socket and stubborn-descendant tests verify this behavior.

The bridge has global and per-identity leases, bounded client messages, one send lock for PTY output and heartbeat frames, and explicit cleanup. Because an attached tmux client—not xterm's alternate buffer—owns authoritative pane history, the default terminal gesture uses typed history messages that resolve the safe session target and invoke fixed `copy-mode` and `send-keys -X` argument arrays. An explicit default-off Application Scroll control instead starts at one wheel event per 18 pixels of vertical swipe movement and applies a deterministic 1x–4x multiplier from average gesture velocity, capped at 72 events, through xterm's negotiated mouse protocol. This lets deliberate drags remain precise while fast flicks travel farther in a mouse-aware foreground TUI. Xterm's ordered reports from one synthetic gesture are buffered only during that dispatch and sent together through the existing bounded input serializer, preventing one gesture from amplifying into a rate-limit-exhausting WebSocket message burst. Wheel dispatch, the App Scroll toggle, and application-mode Older/Latest are focus-neutral, so they do not focus xterm's hidden keyboard textarea; connection and typing-oriented controls retain intentional focus. In that mode Older and Latest use the same path as fixed 12-event wheel-up and wheel-down bursts; while off they retain their tmux-history behavior. New sessions remain default-off; an explicit selection is stored on this device by opaque session ID and survives terminal exit, connection loss, reconnect, and reload only for that session until explicitly disabled. Latest and disconnect cancel copy mode entered by the connection only while application scrolling is off. The abstraction permits replacing `forkpty` without changing routes or frontend protocol.

## Runtime containment

The container disables server-GC hard affinity so each GC thread remains
schedulable across the process's available CPUs during asymmetric host
contention. Liveness remains an in-process endpoint, but the probe that observes
it is an image-local shell process started independently by Docker. It records
only bounded failure counters in tmpfs, keyed to the current app process start
identity so a restarted process cannot inherit a prior trip count. Twelve
failures are allowed before the first successful startup and six afterward;
success clears the current count.

At the threshold, the watchdog signals only PID 1 when it is `dotnet`, or the
sole direct PID-1 child when that child is `dotnet`. Ambiguous process ownership
fails closed. This turns a prolonged running-but-unhealthy state into process
exit so the existing restart policy can recover, while the separately owned
host tmux server and its sessions remain alive. The watchdog has no Docker
socket, added capability, host process visibility, tmux command access, or
terminal-content logging.

## Authentication and authorization

The MVP uses an access key only to establish an encrypted, HttpOnly, Secure, SameSite=Strict cookie. The key is supplied through server configuration and is never stored by the PWA. Cookie data-protection keys persist on disk to keep sessions valid across restarts.

Policies are explicit:

- `Read`: inventory, captures, config, and inventory stream.
- `Interact`: create/rename session, pane input, interrupt, and terminal.
- `Admin`: explicitly confirmed single-session termination.

Cookie mutations require an antiforgery header and same-site token cookie.
Creation and termination are rate-limited and audited on both success and
failure. There are no arbitrary command or bulk destructive endpoints.

## UI state

Cards use `100dvh`, safe-area insets, `scroll-snap-type: y mandatory`, and a non-scrollable faded preview so vertical touch movement belongs to the deck. Local storage contains only the active opaque session ID, the duplicate-free recency list of opaque IDs, and a bounded set of opaque session IDs with explicitly enabled App Scroll; terminal content, names, search queries, access keys, cookies, and API results are not stored there.

Realtime snapshots replace records by ID without scrolling the deck. The client
keeps a device-local, duplicate-free MRU list of opaque session IDs updated only
when Terminal is opened from a tile. Ranked live sessions lead the deck;
unranked sessions retain snapshot order, and stale IDs are pruned. Tiles,
previous/next navigation, active position, terminal lookup, and the session rail
all consume this one derived order. The active ID is restored after reload or
resume if it still exists. Terminal mode is lazy-loaded, preserves the selected
card, and exposes an explicit back control. One-finger vertical gestures
navigate bounded tmux history by default; Older and Latest provide explicit
access to the same history controls. The owner may explicitly enable
Application Scroll to route distance- and velocity-scaled swipe movement plus
Older/Latest as bounded mouse-wheel input to the foreground program. That mode
is visibly indicated and persists on this device only for that opaque session
across connection loss, terminal exit, and reload until explicitly disabled.

The main-screen search applies a case-insensitive substring filter to that
already-derived order on every input event, so filtering cannot mutate server
inventory or recency. Tiles, navigation, counts, and the rail consume the same
filtered array. A non-persistent Detached toggle composes with that name filter
using the session attachment metadata without treating detached as abandoned.
A card's quick menu can open a target-naming confirmation before the Admin-only
single-session termination request; cancellation sends nothing and failures
remain visible without optimistic removal. A top-level New action posts only a name; on success the client
promotes the returned opaque ID, enters terminal mode directly from the response,
and refreshes inventory without waiting for polling.

The service worker caches only application-shell GETs. Requests under `/api`, `/ws`, and `/health` are never cached. Each production web build stamps the worker cache identity from the generated root asset graph, allowing the installed PWA to detect a release. Container packaging clears its temporary webroot before copying generated output so obsolete hashed application bundles are not retained. A waiting worker does not activate automatically; the UI announces an update.

## Deliberate compromises

- .NET 10 LTS is the server, test, and container target. Runtime migration is
  kept behavior-neutral and verified before security behavior changes.
- The single-user API-key bootstrap is smaller than adding an external identity provider, while keeping authorization boundaries ready for replacement.
- The small repository-owned native `forkpty`/`exec` boundary avoids running
  managed child code after fork, at the cost of a Linux/glibc build dependency.
- tmux format output is delimiter-based. tmux-local names and titles are treated as untrusted display text, but pathological embedded delimiter/newline values can make a record fail closed with a parse error.
- The audit sink is an owner-only JSON-lines file. Its result is independent of
  the tmux action result so a failed append cannot make an already-applied
  action look safe to retry. Rotation is delegated to systemd/logrotate.

## Extension points

`ISessionAnalyzer`, `ITmuxService`, `IProcessRunner`, `IPseudoTerminalFactory`, `IAuditLogger`, and browser-independent API models allow future local analyzers, multiple host adapters, observer identities, and native clients without replacing tmux.
