#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
artifact_directory="${1:-$repository_root/artifacts/desktop/linux-x64}"
launcher="$artifact_directory/tmuxctl.desktop"
executable="$artifact_directory/tmuxctl"
icon="$artifact_directory/tmuxctl.png"

if [[ ! -x "$executable" || ! -f "$icon" || ! -f "$launcher" ]]; then
  echo "Build the Linux desktop output before installing its launcher:" >&2
  echo "  ./scripts/build-desktop.sh linux-x64" >&2
  exit 66
fi

data_home="${XDG_DATA_HOME:-${HOME:?HOME is required}/.local/share}"
applications_directory="$data_home/applications"
install -d -m 0755 "$applications_directory"
install -m 0644 "$launcher" "$applications_directory/tmuxctl.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "$applications_directory"
fi

echo "Installed tmuxctl launcher: $applications_directory/tmuxctl.desktop"
echo "Open tmuxctl from Applications, then choose Add to Favorites in the Ubuntu dock."
