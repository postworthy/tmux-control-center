# API

The generated OpenAPI document is available at `/openapi/v1.json`. Except for health, login, the OpenAPI document, and the static shell, endpoints require the `Read` policy. JSON uses camel case and string enum values.

## Authentication

### `POST /api/auth/login`

Anonymous, rate-limited. Body:

```json
{ "apiKey": "deployment-secret" }
```

On success, sets the Secure/HttpOnly/Strict owner cookie and returns `204`.

### `GET /api/auth/status`

Returns the authenticated identity. `GET /api/auth/csrf` returns a short-lived request token and sets its HttpOnly cookie. Send that value as `X-CSRF-TOKEN` on every state-changing cookie-authenticated request.

### `POST /api/auth/logout`

Requires CSRF and returns `204`.

## Inventory and capture

- `GET /api/sessions` — complete card-ready session records, including bounded preview.
- `GET /api/sessions/{sessionId}` — one session or `404`.
- `GET /api/sessions/{sessionId}/panes` — panes belonging to the validated session.
- `GET /api/panes/{paneId}/capture?lines=200` — `{ "text": "...", "requestedLines": 200 }`. Lines are clamped to configured bounds.
- `GET /api/config` — non-secret client settings, currently `{ "tmuxPrefix": "C-b" }`.

Session and pane IDs are opaque values such as `s_a1...` and `p_b2...`; raw tmux names and targets are never accepted as route targets.

## Actions

State-changing actions require CSRF, body limits, rate limits, target
re-resolution, and auditing. Actions require `Interact` except the destructive
single-session DELETE, which requires `Admin`.

- `POST /api/sessions` with `{ "name": "benchmark-qwen" }` creates one detached
  session and returns `201` with `{ "id": "s_...", "name": "benchmark-qwen" }`.
- `POST /api/sessions/{sessionId}/rename` with `{ "name": "benchmark-qwen" }`
- `DELETE /api/sessions/{sessionId}` with no body terminates that one session and
  returns `204`; unknown targets return `404` and bounded tmux failures return `503`.
- `POST /api/panes/{paneId}/keys` with `{ "keys": ["enter", "controlC"] }`
- `POST /api/panes/{paneId}/text` with `{ "text": "continue" }`
- `POST /api/panes/{paneId}/interrupt` with no body

Creation accepts only a validated session name and rejects `.` and `:` because
tmux would silently rewrite them to `_`. It invokes a fixed
argument vector equivalent to `tmux new-session -d -P -F '#{session_id}' -s
NAME`; there is no request field for a command, argument, path, environment,
socket, or tmux option. Duplicate names return `409` and bounded tmux failures
return `503`. Text is sent literally to an already-running pane via
`tmux send-keys -l`; it is not executed by a server-side shell. There is no
arbitrary command endpoint. Termination resolves the opaque ID against current
tmux inventory immediately before invoking the fixed separated argument vector
`tmux kill-session -t RAW_ID`; callers cannot provide a raw target, option, or
command.

## WebSockets

Authentication uses the same cookie. The browser `Origin` must match a configured allowed origin outside development.

### `/ws/inventory`

The first text frame is the complete current `InventorySnapshot`; later frames are complete meaningful revisions. The shape is:

```json
{
  "version": 12,
  "updatedAt": "2026-07-30T20:00:00Z",
  "sessions": []
}
```

Clients should reconnect with backoff and use REST when unavailable.

### `/ws/terminal/{sessionId}`

Server-to-browser PTY output is binary UTF-8/terminal data. Server heartbeat frames are text:

```json
{ "type": "ping" }
```

Browser input may be sent as a binary frame or a complete text frame:

```json
{ "type": "input", "data": "ls\r" }
```

Resize:

```json
{ "type": "resize", "cols": 80, "rows": 24 }
```

Bounded tmux history navigation uses typed actions; `pages` is clamped to three:

```json
{ "type": "history", "action": "older", "pages": 1 }
{ "type": "history", "action": "newer", "pages": 1 }
{ "type": "history", "action": "latest" }
```

These actions control tmux copy mode through fixed server-side argument arrays;
they are not terminal text and cannot contain arbitrary tmux commands. Latest,
terminal exit, and disconnect perform copy-mode cleanup when this connection
entered history mode.

Frames larger than the configured limit and fragmented client messages are rejected. The server does not replay terminal output after reconnect; the attached tmux pane redraws naturally.

## Health

- `GET /health/live` — process liveness.
- `GET /health/ready` — verifies the configured executable exists and executes a bounded harmless tmux query. It requires `Read` authorization except from loopback, and no running tmux server is reported as degraded.

Normal error bodies do not include production exception details. `400`, `401`,
`403`, `404`, `409`, `429`, `500`, and `503` have their standard meanings.
