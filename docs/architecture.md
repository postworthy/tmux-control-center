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

The bridge has global and per-identity leases, bounded client messages, one send lock for PTY output and heartbeat frames, and explicit cleanup. Because an attached tmux client—not xterm's alternate buffer—owns authoritative pane history, the default terminal gesture uses typed history messages that resolve the safe session target and invoke fixed `copy-mode` and `send-keys -X` argument arrays. An explicit default-off Application Scroll control instead dispatches one to three wheel events through xterm's negotiated mouse protocol, allowing tmux to pass them to a mouse-aware foreground TUI. The control is browser-memory-only and resets on terminal entry, exit, and connection loss. Latest and disconnect cancel copy mode entered by the connection. The abstraction permits replacing `forkpty` without changing routes or frontend protocol.

## Authentication and authorization

The MVP uses an access key only to establish an encrypted, HttpOnly, Secure, SameSite=Strict cookie. The key is supplied through server configuration and is never stored by the PWA. Cookie data-protection keys persist on disk to keep sessions valid across restarts.

Policies are explicit:

- `Read`: inventory, captures, config, and inventory stream.
- `Interact`: rename, pane input, interrupt, and terminal.
- `Admin`: reserved for future destructive operations.

Cookie mutations require an antiforgery header and same-site token cookie. There are no destructive endpoints.

## UI state

Cards use `100dvh`, safe-area insets, `scroll-snap-type: y mandatory`, and a non-scrollable faded preview so vertical touch movement belongs to the deck. The active opaque session ID is the only value stored in `localStorage`; terminal content, access keys, cookies, and API results are not stored there.

Realtime snapshots replace records by ID without scrolling the deck. The active ID is restored after reload or resume if it still exists. Terminal mode is lazy-loaded, preserves the selected card, and exposes an explicit back control. One-finger vertical gestures navigate bounded tmux history by default; Older and Latest provide explicit access to the same history controls. The owner may temporarily enable Application Scroll to route vertical gestures as bounded mouse-wheel input to the foreground program. That mode is visibly indicated, never persisted, and resets off on connection loss or terminal exit.

The service worker caches only application-shell GETs. Requests under `/api`, `/ws`, and `/health` are never cached. A waiting worker does not activate automatically; the UI announces an update.

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
