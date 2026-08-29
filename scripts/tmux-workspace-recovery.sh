#!/usr/bin/env bash
set -uo pipefail

readonly SNAPSHOT_VERSION=1
readonly SCRIPT_PATH="$(readlink -f -- "${BASH_SOURCE[0]}")"
readonly STATE_DIR="${TMUX_WORKSPACE_STATE_DIR:-${XDG_STATE_HOME:-${HOME:?HOME is required}/.local/state}/tmux-mobile-workspace}"
readonly SNAPSHOT_PATH="$STATE_DIR/workspace.v1.tsv"
readonly REQUEST_PATH="$STATE_DIR/restore.request"
readonly STATUS_PATH="$STATE_DIR/status.v1.tsv"
readonly SAVE_INTERVAL_SECONDS="${TMUX_WORKSPACE_SAVE_INTERVAL_SECONDS:-60}"
readonly REQUEST_POLL_SECONDS="${TMUX_WORKSPACE_REQUEST_POLL_SECONDS:-2}"
readonly FIELD_SEPARATOR=$'\x1f'
readonly FORMAT_SEPARATOR='|:tmux-workspace:|'

tmux_arguments=()
if [[ -n "${TMUX_WORKSPACE_SOCKET_NAME:-}" ]]; then
  tmux_arguments=(-L "$TMUX_WORKSPACE_SOCKET_NAME")
fi

log() {
  printf 'tmux-workspace-recovery: %s\n' "$*" >&2
}

tmux_command() {
  tmux "${tmux_arguments[@]}" "$@"
}

prepare_state_directory() {
  if [[ -L "$STATE_DIR" ]]; then
    log "state directory must not be a symbolic link"
    return 1
  fi
  mkdir -p -- "$STATE_DIR" || return 1
  chmod 0700 -- "$STATE_DIR" || return 1
  [[ "$(stat -c '%u' -- "$STATE_DIR")" == "$(id -u)" ]] || {
    log "state directory must be owned by the recovery user"
    return 1
  }
}

encode_field() {
  printf '%s' "$1" | base64 --wrap=0
}

decode_field() {
  local encoded="$1" decoded_value
  local -n result="$2"
  [[ "$encoded" =~ ^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$ ]] || return 1
  decoded_value="$(printf '%s' "$encoded" | base64 --decode 2>/dev/null)" || return 1
  [[ -n "$decoded_value" && "$decoded_value" != *$'\n'* && "$decoded_value" != *$'\r'* && "$decoded_value" != *"$FIELD_SEPARATOR"* ]] || return 1
  result="$decoded_value"
}

classify_command() {
  case "$1" in
    codex) printf 'codex' ;;
    claude) printf 'claude' ;;
    *) printf 'shell' ;;
  esac
}

atomic_replace() {
  local temporary="$1" destination="$2"
  chmod 0600 -- "$temporary" || return 1
  mv -f -- "$temporary" "$destination"
}

save_snapshot() {
  prepare_state_directory || return 1

  local raw_file snapshot_file
  raw_file="$(mktemp "$STATE_DIR/.capture.XXXXXX")" || return 1
  snapshot_file="$(mktemp "$STATE_DIR/.snapshot.XXXXXX")" || {
    rm -f -- "$raw_file"
    return 1
  }
  chmod 0600 -- "$raw_file" "$snapshot_file"

  local format
  format="#{session_name}${FORMAT_SEPARATOR}#{window_index}${FORMAT_SEPARATOR}#{window_name}${FORMAT_SEPARATOR}#{window_layout}${FORMAT_SEPARATOR}#{window_active}${FORMAT_SEPARATOR}#{pane_index}${FORMAT_SEPARATOR}#{pane_current_path}${FORMAT_SEPARATOR}#{pane_current_command}${FORMAT_SEPARATOR}#{pane_active}"
  if ! tmux_command list-panes -a -F "$format" >"$raw_file" 2>/dev/null || [[ ! -s "$raw_file" ]]; then
    rm -f -- "$raw_file" "$snapshot_file"
    log "no live tmux sessions; retained the previous snapshot"
    return 0
  fi

  printf 'version\t%s\t%s\n' "$SNAPSHOT_VERSION" "$(date +%s)" >"$snapshot_file"
  declare -A seen_sessions=()
  declare -A seen_windows=()
  local line fields session_name window_index window_name layout window_active pane_index cwd command pane_active extra
  local session_key window_key agent session_count=0 window_count=0 pane_count=0
  while IFS= read -r line; do
    fields="${line//"$FORMAT_SEPARATOR"/"$FIELD_SEPARATOR"}"
    IFS="$FIELD_SEPARATOR" read -r session_name window_index window_name layout window_active pane_index cwd command pane_active extra <<<"$fields"
    if [[ -n "${extra:-}" || -z "$session_name" || -z "$window_name" || -z "$cwd" || "$cwd" != /* ||
          ! "$window_index" =~ ^[0-9]+$ || ! "$pane_index" =~ ^[0-9]+$ ||
          ! "$window_active" =~ ^[01]$ || ! "$pane_active" =~ ^[01]$ ||
          "$session_name" == *$'\n'* || "$window_name" == *$'\n'* || "$cwd" == *$'\n'* ||
          -z "$layout" || ${#layout} -gt 4096 ]]; then
      rm -f -- "$raw_file" "$snapshot_file"
      log "tmux metadata failed validation; retained the previous snapshot"
      return 1
    fi
    session_key="$(encode_field "$session_name")" || return 1
    window_key="$session_key:$window_index"
    if [[ -z "${seen_sessions[$session_key]+present}" ]]; then
      printf 'session\t%s\n' "$session_key" >>"$snapshot_file"
      seen_sessions[$session_key]=1
      ((session_count += 1))
    fi
    if [[ -z "${seen_windows[$window_key]+present}" ]]; then
      printf 'window\t%s\t%s\t%s\t%s\t%s\n' \
        "$session_key" "$window_index" "$(encode_field "$window_name")" \
        "$(encode_field "$layout")" "$window_active" >>"$snapshot_file"
      seen_windows[$window_key]=1
      ((window_count += 1))
    fi
    agent="$(classify_command "$command")"
    printf 'pane\t%s\t%s\t%s\t%s\t%s\t%s\n' \
      "$session_key" "$window_index" "$pane_index" "$(encode_field "$cwd")" \
      "$agent" "$pane_active" >>"$snapshot_file"
    ((pane_count += 1))
  done <"$raw_file"
  rm -f -- "$raw_file"

  if (( session_count == 0 || window_count == 0 || pane_count == 0 )); then
    rm -f -- "$snapshot_file"
    log "tmux metadata was empty; retained the previous snapshot"
    return 1
  fi
  atomic_replace "$snapshot_file" "$SNAPSHOT_PATH" || return 1
  log "saved snapshot version $SNAPSHOT_VERSION ($session_count sessions, $window_count windows, $pane_count panes)"
}

pane_restore_command() {
  local agent="$1" quoted_script
  [[ "$agent" == "shell" ]] && return 0
  printf -v quoted_script '%q' "$SCRIPT_PATH"
  printf '%s resume-agent %s' "$quoted_script" "$agent"
}

snapshot_is_secure() {
  [[ -f "$SNAPSHOT_PATH" && ! -L "$SNAPSHOT_PATH" ]] || return 1
  [[ "$(stat -c '%u' -- "$SNAPSHOT_PATH")" == "$(id -u)" ]] || return 1
  local mode
  mode="$(stat -c '%a' -- "$SNAPSHOT_PATH")"
  (( (8#$mode & 077) == 0 ))
}

restore_snapshot() {
  prepare_state_directory || return 13
  if tmux_command has-session 2>/dev/null; then
    log "restore refused because tmux already has live sessions"
    return 10
  fi
  if [[ ! -e "$SNAPSHOT_PATH" ]]; then
    log "restore requested without a saved snapshot"
    return 11
  fi
  if ! snapshot_is_secure; then
    log "snapshot ownership or permissions failed validation"
    return 12
  fi

  local first=true kind a b c d e f extra decoded
  local -a session_keys=() session_names=()
  local -a window_sessions=() window_indices=() window_names=() window_layouts=() window_active=()
  local -a pane_sessions=() pane_windows=() pane_indices=() pane_directories=() pane_agents=() pane_active=()
  declare -A known_sessions=() known_windows=() known_panes=()

  while IFS=$'\t' read -r kind a b c d e f extra; do
    if $first; then
      first=false
      if [[ "$kind" != "version" || "$a" != "$SNAPSHOT_VERSION" || ! "${b:-}" =~ ^[0-9]+$ || -n "${c:-}" ]]; then
        log "snapshot header failed validation"
        return 12
      fi
      continue
    fi
    case "$kind" in
      session)
        [[ -n "${a:-}" && -z "${b:-}" ]] || return 12
        decode_field "$a" decoded || return 12
        [[ -z "${known_sessions[$a]+present}" ]] || return 12
        known_sessions[$a]=1
        session_keys+=("$a")
        session_names+=("$decoded")
        ;;
      window)
        [[ -n "${a:-}" && "${b:-}" =~ ^[0-9]+$ && -n "${c:-}" && -n "${d:-}" && "${e:-}" =~ ^[01]$ && -z "${f:-}" ]] || return 12
        [[ -n "${known_sessions[$a]+present}" && -z "${known_windows[$a:$b]+present}" ]] || return 12
        decode_field "$c" decoded || return 12
        window_names+=("$decoded")
        decode_field "$d" decoded || return 12
        [[ ${#decoded} -le 4096 ]] || return 12
        window_layouts+=("$decoded")
        window_sessions+=("$a")
        window_indices+=("$b")
        window_active+=("$e")
        known_windows[$a:$b]=1
        ;;
      pane)
        [[ -n "${a:-}" && "${b:-}" =~ ^[0-9]+$ && "${c:-}" =~ ^[0-9]+$ && -n "${d:-}" && "${e:-}" =~ ^(shell|codex|claude)$ && "${f:-}" =~ ^[01]$ && -z "${extra:-}" ]] || return 12
        [[ -n "${known_windows[$a:$b]+present}" && -z "${known_panes[$a:$b:$c]+present}" ]] || return 12
        decode_field "$d" decoded || return 12
        [[ "$decoded" == /* ]] || return 12
        pane_sessions+=("$a")
        pane_windows+=("$b")
        pane_indices+=("$c")
        pane_directories+=("$decoded")
        pane_agents+=("$e")
        pane_active+=("$f")
        known_panes[$a:$b:$c]=1
        ;;
      *) return 12 ;;
    esac
  done <"$SNAPSHOT_PATH"

  if $first || (( ${#session_keys[@]} == 0 || ${#window_sessions[@]} == 0 || ${#pane_sessions[@]} == 0 )); then
    log "snapshot has no restorable workspace"
    return 12
  fi

  local window_key pane_key pane_command session_id window_id pane_id output cwd
  local -a created_sessions=()
  declare -A restored_sessions=() restored_windows=() restored_panes=()
  local session_position window_position pane_position first_window first_pane

  cleanup_partial_restore() {
    local created
    for created in "${created_sessions[@]}"; do
      tmux_command kill-session -t "$created" >/dev/null 2>&1 || true
    done
  }

  for session_position in "${!session_keys[@]}"; do
    first_window=true
    for window_position in "${!window_sessions[@]}"; do
      [[ "${window_sessions[$window_position]}" == "${session_keys[$session_position]}" ]] || continue
      window_key="${window_sessions[$window_position]}:${window_indices[$window_position]}"
      first_pane=true
      for pane_position in "${!pane_sessions[@]}"; do
        pane_key="${pane_sessions[$pane_position]}:${pane_windows[$pane_position]}"
        [[ "$pane_key" == "$window_key" ]] || continue
        cwd="${pane_directories[$pane_position]}"
        [[ -d "$cwd" ]] || cwd="${HOME:?HOME is required}"
        pane_command="$(pane_restore_command "${pane_agents[$pane_position]}")"
        if $first_window && $first_pane; then
          local -a create_args=(new-session -d -P -F '#{session_id}' -s "${session_names[$session_position]}" -n "${window_names[$window_position]}" -c "$cwd")
          [[ -n "$pane_command" ]] && create_args+=("$pane_command")
          session_id="$(tmux_command "${create_args[@]}" 2>/dev/null)" || {
            cleanup_partial_restore
            log "restore failed while creating a session"
            return 13
          }
          created_sessions+=("$session_id")
          restored_sessions["${session_keys[$session_position]}"]="$session_id"
          window_id="$(tmux_command display-message -p -t "$session_id" '#{window_id}')" || { cleanup_partial_restore; return 13; }
          pane_id="$(tmux_command display-message -p -t "$window_id" '#{pane_id}')" || { cleanup_partial_restore; return 13; }
        elif $first_pane; then
          local -a window_args=(new-window -d -P -F '#{window_id}' -t "${restored_sessions[${session_keys[$session_position]}]}:" -n "${window_names[$window_position]}" -c "$cwd")
          [[ -n "$pane_command" ]] && window_args+=("$pane_command")
          window_id="$(tmux_command "${window_args[@]}" 2>/dev/null)" || { cleanup_partial_restore; return 13; }
          pane_id="$(tmux_command display-message -p -t "$window_id" '#{pane_id}')" || { cleanup_partial_restore; return 13; }
        else
          local -a pane_args=(split-window -d -P -F '#{pane_id}' -t "$window_id" -c "$cwd")
          [[ -n "$pane_command" ]] && pane_args+=("$pane_command")
          pane_id="$(tmux_command "${pane_args[@]}" 2>/dev/null)" || { cleanup_partial_restore; return 13; }
        fi
        restored_windows[$window_key]="$window_id"
        restored_panes["$window_key:${pane_indices[$pane_position]}"]="$pane_id"
        first_pane=false
      done
      $first_pane && { cleanup_partial_restore; return 12; }
      tmux_command select-layout -t "$window_id" "${window_layouts[$window_position]}" >/dev/null 2>&1 || {
        cleanup_partial_restore
        log "restore failed while applying a pane layout"
        return 13
      }
      first_window=false
    done
    $first_window && { cleanup_partial_restore; return 12; }
  done

  for pane_position in "${!pane_sessions[@]}"; do
    [[ "${pane_active[$pane_position]}" == 1 ]] || continue
    tmux_command select-pane -t "${restored_panes[${pane_sessions[$pane_position]}:${pane_windows[$pane_position]}:${pane_indices[$pane_position]}]}" >/dev/null 2>&1 || {
      cleanup_partial_restore
      return 13
    }
  done
  for window_position in "${!window_sessions[@]}"; do
    [[ "${window_active[$window_position]}" == 1 ]] || continue
    tmux_command select-window -t "${restored_windows[${window_sessions[$window_position]}:${window_indices[$window_position]}]}" >/dev/null 2>&1 || {
      cleanup_partial_restore
      return 13
    }
  done

  log "restored snapshot version $SNAPSHOT_VERSION (${#session_keys[@]} sessions, ${#window_sessions[@]} windows, ${#pane_sessions[@]} panes)"
}

write_status() {
  local state="$1" request_id="$2" count="${3:-0}" temporary
  temporary="$(mktemp "$STATE_DIR/.status.XXXXXX")" || return 1
  printf '1\t%s\t%s\t%s\t%s\n' "$state" "$(date +%s)" "$count" "$request_id" >"$temporary"
  atomic_replace "$temporary" "$STATUS_PATH"
}

consume_restore_request() {
  [[ -f "$REQUEST_PATH" && ! -L "$REQUEST_PATH" ]] || return 0
  [[ "$(stat -c '%u' -- "$REQUEST_PATH")" == "$(id -u)" ]] || {
    log "ignored restore request with invalid ownership"
    return 0
  }
  local processing="$STATE_DIR/.restore.processing" version request_id requested_at extra
  mv -f -- "$REQUEST_PATH" "$processing" || return 1
  IFS=$'\t' read -r version request_id requested_at extra <"$processing" || true
  rm -f -- "$processing"
  if [[ "$version" != 1 || ! "$request_id" =~ ^[0-9a-fA-F-]{36}$ || ! "$requested_at" =~ ^[0-9]+$ || -n "${extra:-}" ]]; then
    write_status invalid-request "00000000-0000-0000-0000-000000000000" 0
    log "rejected malformed restore request"
    return 0
  fi

  restore_snapshot
  local result=$?
  case "$result" in
    0)
      local count
      count="$(tmux_command list-sessions -F '#{session_id}' 2>/dev/null | wc -l)"
      write_status restored "$request_id" "$count"
      ;;
    10) write_status blocked-live-sessions "$request_id" 0 ;;
    11) write_status no-snapshot "$request_id" 0 ;;
    12) write_status invalid-snapshot "$request_id" 0 ;;
    *) write_status failed "$request_id" 0 ;;
  esac
}

run_daemon() {
  [[ "$SAVE_INTERVAL_SECONDS" =~ ^[0-9]+$ ]] && (( SAVE_INTERVAL_SECONDS >= 5 && SAVE_INTERVAL_SECONDS <= 3600 )) || {
    log "save interval must be between 5 and 3600 seconds"
    return 1
  }
  [[ "$REQUEST_POLL_SECONDS" =~ ^[0-9]+$ ]] && (( REQUEST_POLL_SECONDS >= 1 && REQUEST_POLL_SECONDS <= 10 )) || {
    log "request poll interval must be between 1 and 10 seconds"
    return 1
  }
  prepare_state_directory || return 1
  trap 'save_snapshot || true; exit 0' TERM INT
  log "daemon started; restore requires an explicit app request"
  local last_save=0 now
  while true; do
    consume_restore_request || true
    now="$(date +%s)"
    if (( now - last_save >= SAVE_INTERVAL_SECONDS )); then
      save_snapshot || true
      last_save="$now"
    fi
    sleep "$REQUEST_POLL_SECONDS" &
    wait $!
  done
}

resume_agent() {
  local agent="${1:-}" shell_path="${SHELL:-/bin/sh}" status=127
  case "$agent" in
    codex)
      if command -v codex >/dev/null 2>&1; then codex resume --last; status=$?; fi
      ;;
    claude)
      if command -v claude >/dev/null 2>&1; then claude --continue; status=$?; fi
      ;;
    *) log "refused unsupported agent resume" ;;
  esac
  log "agent process exited with status $status; opening a login shell"
  exec "$shell_path" -l
}

case "${1:-}" in
  save) save_snapshot ;;
  restore) restore_snapshot ;;
  run) run_daemon ;;
  resume-agent) resume_agent "${2:-}" ;;
  *)
    printf 'Usage: %s {save|restore|run|resume-agent codex|resume-agent claude}\n' "${0##*/}" >&2
    exit 64
    ;;
esac
