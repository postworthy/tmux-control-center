# Desktop guide

[Back to the overview](../README.md)

The tmuxctl desktop client connects to an existing server on Linux or macOS.
Have its HTTPS address and login key ready. If you need a server first, follow
[Linux setup](../deploy/docker/README.md) or [Mac setup](macos-server.md).

## Build and launch

Install Git and the .NET 10 SDK on your build machine, then get the source:

```bash
git clone https://github.com/postworthy/tmux-control-center.git
cd tmux-control-center
```

Run the commands below from the repository root. The outputs include the .NET
runtime, so the machine running the finished client needs no separate .NET
installation. It still needs the operating system's native web view. The client
does not install or start a server.

### Ubuntu x64

On Ubuntu 24.04, install the WebKitGTK runtime, build, and register the launcher:

```bash
sudo apt-get install libwebkit2gtk-4.1-0
./scripts/build-desktop.sh linux-x64
./scripts/install-desktop-launcher.sh
./artifacts/desktop/linux-x64/tmuxctl
```

Open tmuxctl from Applications, then choose **Add to Favorites** from its dock
icon. The launcher and native window use the same icon as the mobile app.

### Apple Silicon macOS

```bash
./scripts/build-desktop.sh osx-arm64
open artifacts/desktop/osx-arm64/tmuxctl.app
```

Copy the app bundle to Applications and add it to the Dock if desired. Delivery
is currently from source: there are no published binary releases, `.deb`/`.dmg`
installers, signing, or notarization. Intel macOS and Windows are deferred.

## Connect to a server

Launch without arguments to open the server chooser. Add a label and the server's
HTTPS origin (for example, `https://your-host.your-tailnet.ts.net`), select it,
and sign in with its login key. Use **Servers** in the sidebar to return to the
chooser and switch hosts. Each server has its own authentication.

Saved profiles contain labels and origins, never login keys or terminal content.
They live in the operating system's application-data directory with owner-only
permissions; on a conventional Ubuntu setup this is
`~/.config/tmuxctl/profiles.json`. HTTP is accepted only for loopback development.

An incompatible server returns you to the chooser with an update message. If the
initial page cannot load, the client returns after 12 seconds with network, server,
and TLS troubleshooting context. Check that the server URL works in a browser
and that you can reach its private network. See [architecture](architecture.md)
for the capability check and same-origin authentication design.

## Sessions and layouts

Select a session to open a terminal tab. Switching tabs keeps sessions attached;
closing a tab or window detaches its clients and leaves tmux running. Transient
network loss reconnects with bounded backoff. Use the pop-out control to open a
session in an independent native window.

The sidebar collapses to an icon rail. Its pencil action renames a session and
updates open tabs without reconnecting. Clicking **×** and confirming the named
session kills it. Typing `exit` in the terminal follows ordinary tmux semantics.

Drag a tab to one of five labeled targets: left, right, top, bottom, or center.
Edge targets split the complete current layout. The center **Single view** target,
or the sidebar action of that name, returns open tabs to one row. These layouts
do not create tmux panes or additional attachments. tmux windows and panes remain
available through normal tmux interaction inside the terminal. Terminals refit
when selected and when the window is maximized or enters fullscreen.

## Keyboard, mouse, and clipboard

| Action | Control |
| --- | --- |
| Previous / next session tab | Ctrl+PageUp / Ctrl+PageDown |
| Close active tab (detach) | Ctrl+Shift+W |
| Copy terminal selection | Ctrl+Shift+C; Command+C also works on macOS |
| Guarded paste | Ctrl+Shift+V |
| Change terminal text size | Ctrl+mouse-wheel |
| Navigate tmux history | Mouse-wheel without Ctrl |
| Select text when tmux captures the mouse | Shift+drag |

Completing a mouse selection copies it to the system clipboard, including tmux
copy-mode selections sent through OSC 52. You can paste into other desktop apps.
Copy is limited to 128 KiB; terminal requests to read the clipboard are ignored.
Paste uses the guarded confirmation flow. Font zoom has bounded limits.

For testing on actual hardware, use the [desktop acceptance checklist](desktop-acceptance.md).
A successful source build does not establish clipboard, fullscreen, sleep/wake,
or reconnect acceptance on a physical device.

## When a sign-in expires

If an action needs authentication again, the desktop client and PWA show an
access-key prompt on the current server. Enter your key, then retry the action;
you do not need to restart the app or choose the server again. A pending new
session name is retained. Open terminals detach while the prompt is shown and
reconnect after sign-in; the underlying tmux sessions keep running. The existing
eight-hour sliding login policy still applies, and passwords are not saved.
