#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
watchdog="$repository_root/deploy/docker/healthcheck-watchdog.sh"
test_root="$(mktemp -d)"
trap 'rm -rf -- "$test_root"' EXIT

if TMUX_MOBILE_WATCHDOG_STATE_DIR="$test_root/invalid" \
   TMUX_MOBILE_WATCHDOG_STARTUP_FAILURES=0 \
   sh "$watchdog" file:///etc/hosts >"$test_root/invalid.out" 2>&1; then
    echo "Watchdog accepted an invalid failure threshold." >&2
    exit 1
fi
grep -q 'watchdog configuration is invalid' "$test_root/invalid.out"

state_dir="$test_root/state"
TMUX_MOBILE_WATCHDOG_STATE_DIR="$state_dir" \
TMUX_MOBILE_WATCHDOG_STARTUP_FAILURES=2 \
TMUX_MOBILE_WATCHDOG_STEADY_FAILURES=2 \
sh "$watchdog" file:///etc/hosts
test -e "$state_dir/live-once"
test ! -e "$state_dir/failures"

if TMUX_MOBILE_WATCHDOG_STATE_DIR="$state_dir" \
   TMUX_MOBILE_WATCHDOG_STARTUP_FAILURES=2 \
   TMUX_MOBILE_WATCHDOG_STEADY_FAILURES=2 \
   sh "$watchdog" http://127.0.0.1:9 >"$test_root/failure.out" 2>&1; then
    echo "Watchdog unexpectedly accepted a failed liveness probe." >&2
    exit 1
fi
test "$(cat "$state_dir/failures")" = 1
test -e "$state_dir/live-once"

echo "healthcheck-watchdog tests passed"
