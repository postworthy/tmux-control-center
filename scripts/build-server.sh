#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
case "$(uname -s):$(uname -m)" in
  Linux:x86_64) runtime_identifier=linux-x64 ;;
  Darwin:arm64) runtime_identifier=osx-arm64 ;;
  *) echo "Native server builds support Linux x64 and macOS arm64." >&2; exit 69 ;;
esac
output_directory="${1:-$repository_root/artifacts/server/$runtime_identifier}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$repository_root/.dotnet}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$repository_root/.nuget/packages}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
npm --prefix "$repository_root/src/TmuxMobile.Web" ci
npm --prefix "$repository_root/src/TmuxMobile.Web" run build
# Web assets must exist before publish evaluates the Web SDK content items.
dotnet publish "$repository_root/src/TmuxMobile.Server/TmuxMobile.Server.csproj" \
  --configuration Release --runtime "$runtime_identifier" --self-contained true \
  --output "$output_directory"
install -d "$output_directory/recovery"
install -m 0755 "$repository_root/scripts/tmux-workspace-recovery.sh" "$output_directory/recovery/"
install -m 0644 "$repository_root/scripts/workspace-platform.sh" "$output_directory/recovery/"
cc -O2 -Wall -Wextra -Werror -o "$output_directory/recovery/tmux-workspace-lock" \
  "$repository_root/scripts/native/tmux_workspace_lock.c"
echo "Native server output: $output_directory"
