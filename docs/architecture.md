# Architecture

## Boundaries

```text
React PWA
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

`TmuxMobile.Core` contains stable models, interfaces, validation, sanitization, key encoding, inventory comparison, and status rules. It has no ASP.NET Core or process-execution dependency.

`TmuxMobile.Infrastructure` owns direct process and operating-system behavior. `ProcessRunner` always uses `ProcessStartInfo.ArgumentList`, never a shell command string, drains stdout and stderr independently, enforces cancellation/timeouts, and retains bounded output. `TmuxService` uses explicit tab-delimited tmux format strings and maps raw `$session`/`%pane` targets to stable SHA-256-derived opaque IDs.

`TmuxMobile.Server` composes options, policies, routes, shared polling, WebSockets, health checks, security middleware, and static PWA hosting. The frontend consumes only domain JSON and never sees a raw tmux target.

## Inventory and previews

One singleton background service polls metadata. Connected browsers do not create polling loops. `InventoryStore` publishes only snapshots whose domain records changed and sends the current complete snapshot to each new client. REST remains independent of the realtime channel.

`TmuxService` caches previews per active pane using the separate preview interval. Metadata can poll every three seconds without capturing every pane every time. Captures are line- and byte-bounded; ANSI/OSC/control sequences are removed before JSON serialization. React renders output as text inside `<pre>` and never uses HTML injection.

## Terminal lifecycle

The server validates the opaque session ID against current tmux state before allocating a terminal. The Linux adapter calls `forkpty`, immediately executes:

```text
/usr/bin/tmux [-L configured-socket] attach-session -t validated-raw-target
```

The PTY master is bridged to xterm.js. Input and terminal output are never logged. Resize uses `TIOCSWINSZ`. A connection ends on browser close, cancellation, idle timeout, PTY exit, application shutdown, or network loss. Disposal signals only the attached tmux client child; tmux's server and session remain alive. A real isolated-socket test verifies this behavior.

The bridge has global and per-identity leases, bounded client messages, one send lock for PTY output and heartbeat frames, and explicit cleanup. Because an attached tmux client—not xterm's alternate buffer—owns authoritative pane history, typed terminal history messages resolve the safe session target and invoke fixed `copy-mode` and `send-keys -X` argument arrays. One completed vertical gesture produces one bounded operation; Latest and disconnect cancel copy mode entered by the connection. The abstraction permits replacing `forkpty` without changing routes or frontend protocol.

## Authentication and authorization

The MVP uses an access key only to establish an encrypted, HttpOnly, Secure, SameSite=Strict cookie. The key is supplied through server configuration and is never stored by the PWA. Cookie data-protection keys persist on disk to keep sessions valid across restarts.

Policies are explicit:

- `Read`: inventory, captures, config, and inventory stream.
- `Interact`: rename, pane input, interrupt, and terminal.
- `Admin`: reserved for future destructive operations.

Cookie mutations require an antiforgery header and same-site token cookie. There are no destructive endpoints.

## UI state

Cards use `100dvh`, safe-area insets, `scroll-snap-type: y mandatory`, and a non-scrollable faded preview so vertical touch movement belongs to the deck. The active opaque session ID is the only value stored in `localStorage`; terminal content, access keys, cookies, and API results are not stored there.

Realtime snapshots replace records by ID without scrolling the deck. The active ID is restored after reload or resume if it still exists. Terminal mode is lazy-loaded, preserves the selected card, and exposes an explicit back control. One-finger vertical gestures scroll only the bounded browser-side xterm buffer; Older and Latest controls provide the same navigation without gestures and never write to the PTY.

The service worker caches only application-shell GETs. Requests under `/api`, `/ws`, and `/health` are never cached. A waiting worker does not activate automatically; the UI announces an update.

## Deliberate compromises

- .NET 8 LTS is targeted because it is the stable SDK installed in this development environment.
- The single-user API-key bootstrap is smaller than adding an external identity provider, while keeping authorization boundaries ready for replacement.
- Linux `forkpty` avoids a third-party native PTY dependency, at the cost of Linux/glibc specificity.
- tmux format output is delimiter-based. tmux-local names and titles are treated as untrusted display text, but pathological embedded delimiter/newline values can make a record fail closed with a parse error.
- The audit sink is a permission-restricted JSON-lines file. Rotation is delegated to systemd/logrotate in the MVP.

## Extension points

`ISessionAnalyzer`, `ITmuxService`, `IProcessRunner`, `IPseudoTerminalFactory`, `IAuditLogger`, and browser-independent API models allow future local analyzers, multiple host adapters, observer identities, and native clients without replacing tmux.
