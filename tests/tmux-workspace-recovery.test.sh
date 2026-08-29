#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
recovery="$repository_root/scripts/tmux-workspace-recovery.sh"
test_root="$(mktemp -d)"
socket_name="tmux-mobile-recovery-test-$$"
state_dir="$test_root/state"
project_a="$test_root/project-a"
project_b="$test_root/project-b"
marker_dir="$test_root/markers"
mkdir -p "$project_a" "$project_b" "$marker_dir"

cleanup() {
  tmux -L "$socket_name" kill-server >/dev/null 2>&1 || true
  rm -rf -- "$test_root"
}
trap cleanup EXIT

cc -O2 -Wall -Wextra -Werror -x c -o "$test_root/agent-helper" - <<'C'
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
int main(int argc, char **argv) {
    const char *markers = getenv("TMUX_RECOVERY_TEST_MARKERS");
    const char *name = strrchr(argv[0], '/');
    name = name ? name + 1 : argv[0];
    if (markers != NULL && argc >= 2) {
        char path[4096];
        if (snprintf(path, sizeof(path), "%s/%s", markers, name) >= (int)sizeof(path)) return 2;
        FILE *file = fopen(path, "w");
        if (file == NULL) return 3;
        for (int i = 1; i < argc; i++) fprintf(file, "%s%s", i == 1 ? "" : " ", argv[i]);
        fputc('\n', file);
        fclose(file);
    }
    sleep(30);
    return 0;
}
C
ln -s "$test_root/agent-helper" "$test_root/codex"
ln -s "$test_root/agent-helper" "$test_root/claude"

export PATH="$test_root:$PATH"
export HOME="$test_root/home"
export SHELL=/bin/bash
export TMUX_WORKSPACE_STATE_DIR="$state_dir"
export TMUX_WORKSPACE_SOCKET_NAME="$socket_name"
export TMUX_RECOVERY_TEST_MARKERS="$marker_dir"
mkdir -p "$HOME"

tmux -L "$socket_name" new-session -d -s alpha -n editor -c "$project_a" "$test_root/codex 30"
tmux -L "$socket_name" split-window -d -t alpha:editor -c "$project_b" "sleep 30"
tmux -L "$socket_name" new-window -d -t alpha: -n assistant -c "$project_b" "$test_root/claude 30"
tmux -L "$socket_name" select-window -t alpha:editor
tmux -L "$socket_name" select-pane -t alpha:editor.1

"$recovery" save
snapshot="$state_dir/workspace.v1.tsv"
[[ -f "$snapshot" ]]
[[ "$(stat -c '%a' "$state_dir")" == 700 ]]
[[ "$(stat -c '%a' "$snapshot")" == 600 ]]
grep -q $'pane\t.*\tcodex\t' "$snapshot"
grep -q $'pane\t.*\tclaude\t' "$snapshot"
grep -q $'pane\t.*\tshell\t' "$snapshot"
! grep -q 'sleep 30' "$snapshot"
! grep -q 'resume --last' "$snapshot"

tmux -L "$socket_name" kill-server
"$recovery" restore

[[ "$(tmux -L "$socket_name" display-message -p -t alpha '#{session_name}')" == alpha ]]
[[ "$(tmux -L "$socket_name" list-windows -t alpha -F '#{window_name}' | sort | tr '\n' ' ')" == "assistant editor " ]]
[[ "$(tmux -L "$socket_name" list-panes -t alpha:editor -F '#{pane_current_path}' | sort -u | tr '\n' ' ')" == "$project_a $project_b " ]]

for _ in $(seq 1 50); do
  [[ -f "$marker_dir/codex" && -f "$marker_dir/claude" ]] && break
  sleep 0.1
done
[[ "$(<"$marker_dir/codex")" == "resume --last" ]]
[[ "$(<"$marker_dir/claude")" == "--continue" ]]

session_ids_before="$(tmux -L "$socket_name" list-sessions -F '#{session_id}')"
if "$recovery" restore; then
  echo "restore unexpectedly accepted live sessions" >&2
  exit 1
else
  [[ "$?" == 10 ]]
fi
[[ "$(tmux -L "$socket_name" list-sessions -F '#{session_id}')" == "$session_ids_before" ]]

tmux -L "$socket_name" kill-server
cp "$snapshot" "$snapshot.good"
printf 'version\t1\t0\npane\tinvalid\n' >"$snapshot"
chmod 0600 "$snapshot"
if "$recovery" restore; then
  echo "restore unexpectedly accepted corrupt state" >&2
  exit 1
else
  [[ "$?" == 12 ]]
fi
! tmux -L "$socket_name" has-session 2>/dev/null
mv "$snapshot.good" "$snapshot"
chmod 0600 "$snapshot"

printf '1\t11111111-1111-1111-1111-111111111111\t%s\n' "$(date +%s)" >"$state_dir/restore.request"
chmod 0600 "$state_dir/restore.request"
TMUX_WORKSPACE_SAVE_INTERVAL_SECONDS=5 "$recovery" run &
daemon_pid=$!
for _ in $(seq 1 50); do
  [[ -f "$state_dir/status.v1.tsv" ]] && grep -q $'\trestored\t' "$state_dir/status.v1.tsv" && break
  sleep 0.1
done
grep -q $'\trestored\t' "$state_dir/status.v1.tsv"
kill "$daemon_pid"
wait "$daemon_pid"
tmux -L "$socket_name" has-session

grep -q '^KillMode=process$' "$repository_root/deploy/systemd/tmux-mobile-workspace@.service"
grep -q '^KillMode=process$' "$repository_root/deploy/systemd/tmux-mobile-workspace.service"
grep -q '^WantedBy=default.target$' "$repository_root/deploy/systemd/tmux-mobile-workspace.service"
grep -q '^User=%i$' "$repository_root/deploy/systemd/tmux-mobile-workspace@.service"

echo "tmux workspace recovery tests passed"
