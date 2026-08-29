#!/usr/bin/env bash
set -euo pipefail

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repo_root=${TMUX_MOBILE_SETUP_ROOT:-$(dirname -- "$script_dir")}
compose_file="$repo_root/compose.tailscale-serve.yaml"
default_env="$repo_root/deploy/docker/.env"
default_key="$repo_root/deploy/docker/access-key.txt"
probe_socket_name=''
probe_cleanup_needed=0

usage() {
  cat <<'EOF'
Usage:
  scripts/first-run-setup.sh preflight
  scripts/first-run-setup.sh write-env --serve-host HOST [options]
  scripts/first-run-setup.sh probe-tmux [--env-file PATH]

write-env options:
  --tailscale-ip IP       Default: first address from `tailscale ip -4`
  --https-port PORT       Default: 8443
  --http-port PORT        Default: 8780
  --image-tag TAG         Default: first-run
  --socket-name NAME      Optional host `tmux -L` socket name
  --env-file PATH         Default: deploy/docker/.env
  --key-file PATH         Default: deploy/docker/access-key.txt
  --force                 Replace both generated files

The generated access key is never printed. The command reports only its path.
EOF
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

need_command() {
  command -v "$1" >/dev/null 2>&1
}

tmux_release() {
  local output version
  output=$(tmux -V) || fail "tmux is installed but 'tmux -V' failed."
  version=${output#tmux }
  [[ "$output" == "tmux $version" && "$version" =~ ^[0-9]+([.][0-9]+)*[a-z]?$ ]] ||
    fail "Unsupported tmux version string: $output"
  printf '%s\n' "$version"
}

validate_host() {
  [[ "$1" =~ ^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?$ && "$1" == *.* ]] ||
    fail "Serve host must be a fully qualified DNS name."
}

validate_ipv4() {
  local ip=$1 part
  IFS=. read -r -a parts <<<"$ip"
  [[ ${#parts[@]} -eq 4 ]] || fail "Invalid Tailscale IPv4 address."
  for part in "${parts[@]}"; do
    [[ "$part" =~ ^[0-9]{1,3}$ && 10#$part -le 255 ]] ||
      fail "Invalid Tailscale IPv4 address."
  done
}

validate_port() {
  [[ "$1" =~ ^[0-9]+$ && 10#$1 -ge 1 && 10#$1 -le 65535 ]] ||
    fail "Port must be between 1 and 65535."
}

validate_token() {
  [[ "$2" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]] || fail "Invalid $1."
}

preflight() {
  local failed=0
  [[ $(uname -s) == Linux ]] || fail "Only Linux hosts are supported."

  if need_command tmux; then
    printf 'tmux: %s\n' "$(tmux -V)"
  else
    printf '%s\n' "tmux: missing (install with the host package manager after approval)" >&2
    failed=1
  fi

  if need_command docker && docker compose version >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    printf '%s\n' "Docker Engine and Compose: available"
  else
    printf '%s\n' "Docker Engine with Compose v2: missing, stopped, or unavailable to this user" >&2
    printf '%s\n' "Install guidance: https://docs.docker.com/engine/install/" >&2
    failed=1
  fi

  if need_command tailscale; then
    if tailscale ip -4 >/dev/null 2>&1; then
      printf '%s\n' "Tailscale: installed and connected"
    else
      printf '%s\n' "Tailscale: installed but not connected; inspect 'tailscale status'" >&2
      failed=1
    fi
  else
    printf '%s\n' "Tailscale: missing" >&2
    printf '%s\n' "Install guidance: https://tailscale.com/download/linux" >&2
    failed=1
  fi

  (( failed == 0 )) || return 1
}

write_env() {
  local serve_host='' tailscale_ip='' https_port=8443 http_port=8780
  local image_tag=first-run socket_name='' env_file=$default_env key_file=$default_key force=0
  local uid gid version origin secret env_tmp key_tmp

  while (($#)); do
    case "$1" in
      --serve-host) [[ $# -ge 2 ]] || fail "--serve-host requires a value"; serve_host=$2; shift 2 ;;
      --tailscale-ip) [[ $# -ge 2 ]] || fail "--tailscale-ip requires a value"; tailscale_ip=$2; shift 2 ;;
      --https-port) [[ $# -ge 2 ]] || fail "--https-port requires a value"; https_port=$2; shift 2 ;;
      --http-port) [[ $# -ge 2 ]] || fail "--http-port requires a value"; http_port=$2; shift 2 ;;
      --image-tag) [[ $# -ge 2 ]] || fail "--image-tag requires a value"; image_tag=$2; shift 2 ;;
      --socket-name) [[ $# -ge 2 ]] || fail "--socket-name requires a value"; socket_name=$2; shift 2 ;;
      --env-file) [[ $# -ge 2 ]] || fail "--env-file requires a value"; env_file=$2; shift 2 ;;
      --key-file) [[ $# -ge 2 ]] || fail "--key-file requires a value"; key_file=$2; shift 2 ;;
      --force) force=1; shift ;;
      *) fail "Unknown write-env option: $1" ;;
    esac
  done

  [[ -n "$serve_host" ]] || fail "--serve-host is required."
  need_command tmux || fail "tmux is missing."
  need_command tailscale || fail "Tailscale is missing."
  need_command od || fail "od is required to generate a secret."
  validate_host "$serve_host"
  [[ -n "$tailscale_ip" ]] || tailscale_ip=$(tailscale ip -4 | sed -n '1p')
  validate_ipv4 "$tailscale_ip"
  validate_port "$https_port"
  validate_port "$http_port"
  validate_token "image tag" "$image_tag"
  [[ -z "$socket_name" ]] || validate_token "tmux socket name" "$socket_name"
  [[ $force -eq 1 || (! -e "$env_file" && ! -e "$key_file") ]] ||
    fail "Refusing to replace existing configuration; inspect paths yourself, then rerun with --force if replacement is intended."

  uid=$(id -u)
  gid=$(id -g)
  version=$(tmux_release)
  origin="https://${serve_host}"
  [[ $https_port -eq 443 ]] || origin="${origin}:${https_port}"
  secret=$(od -An -N32 -tx1 /dev/urandom | tr -d ' \n')
  [[ ${#secret} -eq 64 ]] || fail "Failed to generate a 32-byte access key."

  umask 077
  mkdir -p -- "$(dirname -- "$env_file")" "$(dirname -- "$key_file")"
  install -d -m 0700 "$repo_root/deploy/docker/state/keys" "$repo_root/deploy/docker/state/audit" \
    "$repo_root/deploy/docker/state/workspace"
  env_tmp=$(mktemp "${env_file}.tmp.XXXXXX")
  key_tmp=$(mktemp "${key_file}.tmp.XXXXXX")
  trap 'rm -f -- "${env_tmp:-}" "${key_tmp:-}"' EXIT

  {
    printf 'TAILSCALE_IP=%s\n' "$tailscale_ip"
    printf 'TMUX_MOBILE_HOST=%s\n' "$serve_host"
    printf 'TMUX_MOBILE_ORIGIN=%s\n' "$origin"
    printf 'TMUX_MOBILE_SERVE_HOST=%s\n' "$serve_host"
    printf 'TMUX_MOBILE_SERVE_ORIGIN=%s\n' "$origin"
    printf 'TMUX_MOBILE_HTTPS_PORT=%s\n' "$https_port"
    printf 'TMUX_MOBILE_HTTP_PORT=%s\n' "$http_port"
    printf 'TMUX_MOBILE_IMAGE_TAG=%s\n' "$image_tag"
    printf 'TMUX_VERSION=%s\n' "$version"
    printf 'TMUX_MOBILE_UID=%s\n' "$uid"
    printf 'TMUX_MOBILE_GID=%s\n' "$gid"
    printf 'TMUX_MOBILE_API_KEY=%s\n' "$secret"
    printf 'TMUX_SOCKET_DIR=/tmp/tmux-%s\n' "$uid"
    printf 'TMUX_SOCKET_NAME=%s\n' "$socket_name"
    printf 'TMUX_MOBILE_KEYS_DIR=./deploy/docker/state/keys\n'
    printf 'TMUX_MOBILE_AUDIT_DIR=./deploy/docker/state/audit\n'
    printf 'TMUX_MOBILE_WORKSPACE_DIR=./deploy/docker/state/workspace\n'
    printf 'TMUX_MOBILE_TLS_CERT=./deploy/docker/secrets/tls.crt\n'
    printf 'TMUX_MOBILE_TLS_KEY=./deploy/docker/secrets/tls.key\n'
  } >"$env_tmp"
  printf '%s\n' "$secret" >"$key_tmp"
  chmod 0600 "$env_tmp" "$key_tmp"
  mv -f -- "$env_tmp" "$env_file"
  mv -f -- "$key_tmp" "$key_file"
  trap - EXIT
  unset secret

  printf 'Configuration written to %s (mode 0600).\n' "$env_file"
  printf 'Login key written to %s (mode 0600); it was not printed.\n' "$key_file"
  printf 'To reveal it yourself when logging in: cat %q\n' "$key_file"
  printf 'Pinned container tmux client to host release %s.\n' "$version"
}

read_public_env() {
  local name=$1 file=$2 value
  value=$(sed -n "s/^${name}=//p" "$file" | sed -n '1p')
  [[ -n "$value" ]] || fail "$name is missing from $file"
  printf '%s\n' "$value"
}

cleanup_probe() {
  if [[ $probe_cleanup_needed -eq 1 ]]; then
    if tmux -L "$probe_socket_name" kill-server >/dev/null 2>&1; then
      rm -f -- "/tmp/tmux-$(id -u)/$probe_socket_name"
    else
      printf 'WARNING: isolated tmux probe cleanup failed for %s\n' "$probe_socket_name" >&2
    fi
    probe_cleanup_needed=0
  fi
}

probe_tmux() {
  local env_file=$default_env session_name=compatibility expected actual observed
  while (($#)); do
    case "$1" in
      --env-file) [[ $# -ge 2 ]] || fail "--env-file requires a value"; env_file=$2; shift 2 ;;
      *) fail "Unknown probe-tmux option: $1" ;;
    esac
  done
  [[ -r "$env_file" ]] || fail "Environment file is not readable: $env_file"
  preflight >/dev/null
  expected=$(read_public_env TMUX_VERSION "$env_file")
  [[ "$expected" == "$(tmux_release)" ]] || fail "TMUX_VERSION does not match the host tmux release; regenerate the environment and rebuild."
  probe_socket_name="tmux-mobile-probe-$$-$(od -An -N4 -tx1 /dev/urandom | tr -d ' \n')"
  trap cleanup_probe EXIT
  trap 'exit 130' INT
  trap 'exit 143' TERM
  tmux -L "$probe_socket_name" new-session -d -s "$session_name"
  probe_cleanup_needed=1

  actual=$(docker compose -f "$compose_file" --env-file "$env_file" run --rm --no-deps \
    --entrypoint /usr/bin/tmux app -V)
  [[ "$actual" == "tmux $expected" ]] || fail "Container reports '$actual'; expected 'tmux $expected'. Rebuild before deployment."
  observed=$(docker compose -f "$compose_file" --env-file "$env_file" run --rm --no-deps \
    --entrypoint /usr/bin/tmux app -L "$probe_socket_name" list-sessions -F '#{session_name}')
  [[ "$observed" == "$session_name" ]] || fail "Container could not query the isolated host tmux server."
  cleanup_probe
  trap - EXIT INT TERM
  printf 'Compatibility probe passed: host and container use tmux %s on isolated socket %s.\n' "$expected" "$probe_socket_name"
}

command=${1:-}
[[ -n "$command" ]] || { usage; exit 2; }
shift
case "$command" in
  preflight) (($# == 0)) || fail "preflight takes no options"; preflight ;;
  write-env) write_env "$@" ;;
  probe-tmux) probe_tmux "$@" ;;
  help|-h|--help) usage ;;
  *) usage >&2; fail "Unknown command: $command" ;;
esac
