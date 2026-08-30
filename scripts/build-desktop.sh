#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
runtime_identifier="${1:-}"
output_directory="${2:-$repository_root/artifacts/desktop/$runtime_identifier}"

case "$runtime_identifier" in
  linux-x64|osx-arm64)
    ;;
  *)
    echo "Usage: $0 <linux-x64|osx-arm64> [output-directory]" >&2
    exit 64
    ;;
esac

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$repository_root/.dotnet}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$repository_root/.nuget/packages}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet publish "$repository_root/src/TmuxCtl.Desktop/TmuxCtl.Desktop.csproj" \
  --configuration Release \
  --runtime "$runtime_identifier" \
  --self-contained true \
  --output "$output_directory"

echo "Desktop output: $output_directory"
