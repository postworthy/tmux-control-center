#!/usr/bin/env bash
# Checks startup survival with isolated profiles; physical interaction remains
# a separate acceptance check. Linux requires a display (or xvfb-run).
set -euo pipefail
executable="${1:?Supply the absolute published desktop executable path}"
[[ "$executable" == /* && -x "$executable" ]]
test_root="$(mktemp -d)"
export TMUXCTL_CONFIG_HOME="$test_root/profiles"
unset TMUXCTL_SERVER_URL
app_pid=
cleanup() {
  if [[ -n "$app_pid" ]]; then
    kill "$app_pid" 2>/dev/null || true
    wait "$app_pid" 2>/dev/null || true
  fi
  rm -rf -- "$test_root"
}
trap cleanup EXIT
"$executable" >"$test_root/startup.log" 2>&1 &
app_pid=$!
sleep 5
if ! kill -0 "$app_pid" 2>/dev/null; then
  # This app uses an empty temporary profile store; the log contains no user URLs.
  cat "$test_root/startup.log" >&2
  echo "Desktop exited during startup." >&2
  exit 1
fi
echo "Published desktop survived startup with isolated profiles."
