#!/bin/sh
set -eu

state_dir="${TMUX_MOBILE_WATCHDOG_STATE_DIR:-/tmp/tmux-mobile-watchdog}"
failure_file="$state_dir/failures"
live_once_file="$state_dir/live-once"
identity_file="$state_dir/app-identity"
startup_threshold="${TMUX_MOBILE_WATCHDOG_STARTUP_FAILURES:-12}"
steady_threshold="${TMUX_MOBILE_WATCHDOG_STEADY_FAILURES:-6}"

valid_threshold() {
    case "$1" in
        ''|*[!0-9]*) return 1 ;;
    esac
    [ "$1" -ge 1 ] && [ "$1" -le 100 ]
}

case "$state_dir" in
    /tmp/*) ;;
    *)
        echo "tmux-mobile watchdog state directory must be beneath /tmp" >&2
        exit 1
        ;;
esac

if ! valid_threshold "$startup_threshold" || ! valid_threshold "$steady_threshold"; then
    echo "tmux-mobile watchdog configuration is invalid" >&2
    exit 1
fi

mkdir -p "$state_dir"

resolve_target() {
    target_pid=
    if [ -r /proc/1/comm ] && [ "$(cat /proc/1/comm)" = dotnet ]; then
        target_pid=1
        return
    fi
    if [ ! -r /proc/1/task/1/children ]; then
        return
    fi
    children=$(cat /proc/1/task/1/children)
    set -- $children
    if [ "$#" -ne 1 ]; then
        return
    fi
    candidate="$1"
    case "$candidate" in
        ''|*[!0-9]*) return ;;
    esac
    if [ -r "/proc/$candidate/comm" ] && [ "$(cat "/proc/$candidate/comm")" = dotnet ]; then
        target_pid="$candidate"
    fi
}

process_identity() {
    process_pid="$1"
    process_stat=$(cat "/proc/$process_pid/stat")
    set -- $process_stat
    if [ "$#" -lt 22 ]; then
        return 1
    fi
    shift 21
    printf '%s:%s\n' "$process_pid" "$1"
}

resolve_target
app_identity=
if [ -n "$target_pid" ]; then
    app_identity=$(process_identity "$target_pid" 2>/dev/null || true)
fi
if [ -n "$app_identity" ]; then
    prior_identity=
    if [ -r "$identity_file" ]; then
        read -r prior_identity <"$identity_file" || prior_identity=
    fi
    if [ "$prior_identity" != "$app_identity" ]; then
        rm -f "$failure_file" "$live_once_file"
        printf '%s\n' "$app_identity" >"$identity_file"
    fi
fi

if curl --fail --silent --show-error --output /dev/null --max-time 4 "$@"; then
    : >"$live_once_file"
    rm -f "$failure_file"
    exit 0
fi

failures=0
if [ -r "$failure_file" ]; then
    read -r failures <"$failure_file" || failures=0
    case "$failures" in
        ''|*[!0-9]*) failures=0 ;;
    esac
fi
failures=$((failures + 1))
printf '%s\n' "$failures" >"$failure_file"

threshold="$startup_threshold"
phase=startup
if [ -e "$live_once_file" ]; then
    threshold="$steady_threshold"
    phase=steady
fi

if [ "$failures" -lt "$threshold" ]; then
    exit 1
fi

resolve_target
current_identity=
if [ -n "$target_pid" ]; then
    current_identity=$(process_identity "$target_pid" 2>/dev/null || true)
fi
if [ -z "$target_pid" ] || [ -z "$app_identity" ] || [ "$current_identity" != "$app_identity" ]; then
    echo "tmux-mobile watchdog reached $phase failure threshold but refused an ambiguous process target" >&2
    exit 1
fi

echo "tmux-mobile watchdog reached $phase failure threshold; terminating the unresponsive app" >&2
kill -TERM "$target_pid" 2>/dev/null || true

remaining=3
while [ "$remaining" -gt 0 ] && [ -e "/proc/$target_pid" ]; do
    sleep 1
    remaining=$((remaining - 1))
done

if [ -e "/proc/$target_pid" ]; then
    kill -KILL "$target_pid" 2>/dev/null || true
fi

exit 1
