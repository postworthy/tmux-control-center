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

publish_directory="$output_directory"
if [[ "$runtime_identifier" == osx-arm64 ]]; then
  publish_directory="$output_directory/tmuxctl.app/Contents/MacOS"
fi

dotnet publish "$repository_root/src/TmuxCtl.Desktop/TmuxCtl.Desktop.csproj" \
  --configuration Release \
  --runtime "$runtime_identifier" \
  --self-contained true \
  --output "$publish_directory"

if [[ "$runtime_identifier" == linux-x64 ]]; then
  executable_path="$output_directory/tmuxctl"
  icon_path="$output_directory/tmuxctl.png"
  if [[ "$executable_path" =~ [\"\&\|] || "$icon_path" =~ [\"\&\|] ||
        "$executable_path" == *$'\n'* || "$executable_path" == *$'\r'* ||
        "$icon_path" == *$'\n'* || "$icon_path" == *$'\r'* ]]; then
    echo "Desktop output path cannot contain a quote, ampersand, pipe, or newline." >&2
    exit 65
  fi
  sed -e "s|@TMUXCTL_EXECUTABLE@|$executable_path|g" \
      -e "s|@TMUXCTL_ICON@|$icon_path|g" \
      "$repository_root/src/TmuxCtl.Desktop/Packaging/linux/tmuxctl.desktop.in" \
      > "$output_directory/tmuxctl.desktop"
else
  resources_directory="$output_directory/tmuxctl.app/Contents/Resources"
  mkdir -p "$resources_directory"
  install -m 0644 "$repository_root/src/TmuxCtl.Desktop/Packaging/macos/Info.plist" \
    "$output_directory/tmuxctl.app/Contents/Info.plist"
  install -m 0644 "$repository_root/src/TmuxCtl.Desktop/Assets/tmuxctl.icns" \
    "$resources_directory/tmuxctl.icns"
fi

echo "Desktop output: $output_directory"
