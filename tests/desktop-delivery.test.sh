#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

grep -Fq '.workspace-layout > .workspace-group, .workspace-layout > .workspace-split { flex: 1 1 0; }' \
  "$repository_root/src/TmuxMobile.Web/desktop/styles.css"
grep -Fq 'Link="tmuxctl.png"' "$repository_root/src/TmuxCtl.Desktop/TmuxCtl.Desktop.csproj"
grep -Fq '.SetIconFile(icon)' "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq 'StartupWMClass=Tmuxctl' \
  "$repository_root/src/TmuxCtl.Desktop/Packaging/linux/tmuxctl.desktop.in"
grep -Fq '<string>tmuxctl.icns</string>' \
  "$repository_root/src/TmuxCtl.Desktop/Packaging/macos/Info.plist"
file "$repository_root/src/TmuxCtl.Desktop/Assets/tmuxctl.icns" | grep -Fq 'Mac OS X icon'
bash -n "$repository_root/scripts/build-desktop.sh"
bash -n "$repository_root/scripts/install-desktop-launcher.sh"

echo "desktop delivery tests passed"
