#!/usr/bin/env bash
set -euo pipefail

bash tests/first-run-setup.test.sh

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$repository_root/.dotnet}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$repository_root/.nuget/packages}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet restore TmuxMobile.sln
dotnet test TmuxMobile.sln --no-restore
npm --prefix src/TmuxMobile.Web run typecheck
npm --prefix src/TmuxMobile.Web run test:unit
docker compose --env-file deploy/docker/.env.example config --quiet

missing_tmux_env=$(mktemp)
trap 'rm -f -- "$missing_tmux_env"' EXIT
grep -v '^TMUX_VERSION=' deploy/docker/.env.example >"$missing_tmux_env"
if docker compose --env-file "$missing_tmux_env" config --quiet >/dev/null 2>&1; then
  echo "Compose unexpectedly accepted a missing TMUX_VERSION." >&2
  exit 1
fi
rm -f -- "$missing_tmux_env"
trap - EXIT

if env -u TAILSCALE_IP docker compose \
  --env-file deploy/docker/.env.missing-tailscale.example config --quiet \
  >/dev/null 2>&1; then
  echo "Compose unexpectedly accepted a missing TAILSCALE_IP." >&2
  exit 1
fi
