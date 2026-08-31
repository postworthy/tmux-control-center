#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

grep -Fq '.workspace-layout > .workspace-group, .workspace-layout > .workspace-split { flex: 1 1 0; }' \
  "$repository_root/src/TmuxMobile.Web/desktop/styles.css"
[[ $(grep -o 'className="drop-guidance"' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopWorkspace.tsx" | wc -l) -eq 1 ]]
grep -Fq 'WORKSPACE_DROP_ZONES.map' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopWorkspace.tsx"
grep -Fq 'Reset to single view' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'const suppressBrowserContextMenu = (event: MouseEvent) => event.preventDefault();' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopTerminal.tsx"
! grep -Fq 'contextMenuCallbackRef' "$repository_root/src/TmuxMobile.Web/desktop/DesktopTerminal.tsx"
! grep -Fq 'terminal-menu' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'renameSession(session.id, name)' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'aria-label={`Rename ${session.name}`}' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'setPendingKill(session)' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'Kill session' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
! grep -Fq 'Type the session name to confirm' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'Link="tmuxctl.png"' "$repository_root/src/TmuxCtl.Desktop/TmuxCtl.Desktop.csproj"
grep -Fq '.SetIconFile(icon)' "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq '.RegisterSizeChangedHandler((sender, _) => NotifyNativeGeometryChanged(sender))' \
  "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq '.RegisterMaximizedHandler(NotifyNativeGeometryChanged)' \
  "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq '.RegisterRestoredHandler(NotifyNativeGeometryChanged)' \
  "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq 'bridge.receiveMessage?.' "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'window.addEventListener(NATIVE_WINDOW_GEOMETRY_EVENT, viewportChanged)' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopTerminal.tsx"
grep -Fq 'bridge.sendMessage?.(JSON.stringify({ type: "toggleFullscreen" }))' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'window.SetFullScreen(!window.FullScreen);' \
  "$repository_root/src/TmuxCtl.Desktop/Program.cs"
grep -Fq "send({type:'toggleFullscreen'})" \
  "$repository_root/src/TmuxCtl.Desktop/ProfileChooserPage.cs"
grep -Fq 'MAXIMUM_TERMINAL_COLUMNS = 2048' \
  "$repository_root/src/TmuxMobile.Web/desktop/terminalLayout.ts"
grep -Fq 'boundedTerminalGrid(terminal.cols, terminal.rows)' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopTerminal.tsx"
grep -Fq 'MaximumColumns = 2048' \
  "$repository_root/src/TmuxMobile.Core/Models.cs"
grep -Fq 'if (socketRef.current !== socket) return;' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopTerminal.tsx"
grep -Fq 'if (socket !== current) return;' \
  "$repository_root/src/TmuxMobile.Web/desktop/DesktopApp.tsx"
grep -Fq 'StartupWMClass=Tmuxctl' \
  "$repository_root/src/TmuxCtl.Desktop/Packaging/linux/tmuxctl.desktop.in"
grep -Fq '<string>tmuxctl.icns</string>' \
  "$repository_root/src/TmuxCtl.Desktop/Packaging/macos/Info.plist"
file "$repository_root/src/TmuxCtl.Desktop/Assets/tmuxctl.icns" | grep -Fq 'Mac OS X icon'
bash -n "$repository_root/scripts/build-desktop.sh"
bash -n "$repository_root/scripts/install-desktop-launcher.sh"

echo "desktop delivery tests passed"
