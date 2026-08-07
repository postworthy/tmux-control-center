#!/usr/bin/env bash
set -euo pipefail

repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
setup_script="$repo_root/scripts/first-run-setup.sh"
test_root=$(mktemp -d)
trap 'rm -rf -- "$test_root"' EXIT
fake_bin="$test_root/bin"
fake_repo="$test_root/repo"
mkdir -p "$fake_bin" "$fake_repo/deploy/docker" "$fake_repo/deploy/docker/state"
cp "$repo_root/compose.tailscale-serve.yaml" "$fake_repo/compose.tailscale-serve.yaml"

write_fake() {
  local name=$1 body=$2
  printf '#!/usr/bin/env bash\n%s\n' "$body" >"$fake_bin/$name"
  chmod +x "$fake_bin/$name"
}

write_fake tmux '
case "${1:-}" in
  -V) printf "tmux 3.4\n" ;;
  -L)
    if [[ "${3:-}" == "new-session" ]]; then printf "created %s\n" "$2" >>"${PROBE_LOG:?}"; fi
    if [[ "${3:-}" == "kill-server" ]]; then printf "cleaned %s\n" "$2" >>"${PROBE_LOG:?}"; fi
    ;;
esac'
write_fake tailscale '
if [[ "${FAKE_TAILSCALE_DISCONNECTED:-0}" == 1 ]]; then exit 1; fi
if [[ "${1:-}" == "ip" && "${2:-}" == "-4" ]]; then printf "100.101.102.103\n"; exit 0; fi
exit 0'
write_fake docker '
if [[ "${FAKE_DOCKER_UNAVAILABLE:-0}" == 1 ]]; then exit 1; fi
if [[ "${1:-}" == "compose" && "${2:-}" == "version" ]]; then exit 0; fi
if [[ "${1:-}" == "info" ]]; then exit 0; fi
if [[ "${1:-}" == "compose" ]]; then
  if [[ "${FAKE_DOCKER_FAIL_VERSION:-0}" == 1 && " $* " == *" app -V "* ]]; then exit 42; fi
  case " $* " in
    *" app -V "*) printf "tmux 3.4\n" ;;
    *" list-sessions "*) printf "compatibility\n" ;;
  esac
  exit 0
fi
exit 1'

export PROBE_LOG="$test_root/probe.log"
PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" \
  "$setup_script" preflight >"$test_root/preflight.out"
grep -q 'Docker Engine and Compose: available' "$test_root/preflight.out"
grep -q 'Tailscale: installed and connected' "$test_root/preflight.out"

if PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" FAKE_DOCKER_UNAVAILABLE=1 \
  "$setup_script" preflight >"$test_root/docker-missing.out" 2>&1; then
  printf '%s\n' 'preflight unexpectedly accepted unavailable Docker' >&2
  exit 1
fi
grep -q 'https://docs.docker.com/engine/install/' "$test_root/docker-missing.out"

if PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" FAKE_TAILSCALE_DISCONNECTED=1 \
  "$setup_script" preflight >"$test_root/tailscale-down.out" 2>&1; then
  printf '%s\n' 'preflight unexpectedly accepted disconnected Tailscale' >&2
  exit 1
fi
grep -q "installed but not connected" "$test_root/tailscale-down.out"

env_file="$fake_repo/deploy/docker/.env"
key_file="$fake_repo/deploy/docker/access-key.txt"
PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" \
  "$setup_script" write-env --serve-host host.example.ts.net \
  --env-file "$env_file" --key-file "$key_file" >"$test_root/write.out"

[[ $(stat -c '%a' "$env_file") == 600 ]]
[[ $(stat -c '%a' "$key_file") == 600 ]]
grep -q '^TAILSCALE_IP=100.101.102.103$' "$env_file"
grep -q '^TMUX_VERSION=3.4$' "$env_file"
grep -q '^TMUX_MOBILE_UID=[0-9][0-9]*$' "$env_file"
key=$(sed -n 's/^TMUX_MOBILE_API_KEY=//p' "$env_file")
[[ "$key" =~ ^[0-9a-f]{64}$ ]]
[[ $(tr -d '\n' <"$key_file") == "$key" ]]
! grep -q "$key" "$test_root/write.out"
unset key

if PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" \
  "$setup_script" write-env --serve-host host.example.ts.net \
  --env-file "$env_file" --key-file "$key_file" >"$test_root/refuse.out" 2>&1; then
  printf '%s\n' 'write-env unexpectedly replaced existing files' >&2
  exit 1
fi
grep -q 'Refusing to replace existing configuration' "$test_root/refuse.out"

if PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" \
  "$setup_script" write-env --serve-host 'bad host' \
  --env-file "$test_root/bad.env" --key-file "$test_root/bad.key" \
  >"$test_root/bad-host.out" 2>&1; then
  printf '%s\n' 'write-env unexpectedly accepted an invalid hostname' >&2
  exit 1
fi
[[ ! -e "$test_root/bad.env" && ! -e "$test_root/bad.key" ]]

PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" \
  "$setup_script" probe-tmux --env-file "$env_file" >"$test_root/probe.out"
grep -q 'Compatibility probe passed' "$test_root/probe.out"
grep -q '^created tmux-mobile-probe-' "$PROBE_LOG"
grep -q '^cleaned tmux-mobile-probe-' "$PROBE_LOG"

: >"$PROBE_LOG"
if PATH="$fake_bin:$PATH" TMUX_MOBILE_SETUP_ROOT="$fake_repo" FAKE_DOCKER_FAIL_VERSION=1 \
  "$setup_script" probe-tmux --env-file "$env_file" >"$test_root/probe-fail.out" 2>&1; then
  printf '%s\n' 'probe-tmux unexpectedly accepted a failed container command' >&2
  exit 1
fi
grep -q '^created tmux-mobile-probe-' "$PROBE_LOG"
grep -q '^cleaned tmux-mobile-probe-' "$PROBE_LOG"
[[ $(wc -l <"$PROBE_LOG") -eq 2 ]]

printf '%s\n' 'first-run setup tests passed'
