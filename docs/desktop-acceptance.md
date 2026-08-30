# Desktop acceptance

Use this checklist on the owner's Ubuntu x64 workstation and Apple Silicon Mac.
It validates the source-built desktop companion against an already-running,
current tmuxctl server. Do not paste login keys, terminal output, profile files,
or private Tailscale names into an issue or test report.

## Record the build

From a clean repository checkout, record the commit and build the target:

```bash
git status --short
git rev-parse HEAD
./scripts/build-desktop.sh linux-x64
./scripts/build-desktop.sh osx-arm64
```

`git status --short` must be empty. The two commands should produce
`artifacts/desktop/linux-x64/tmuxctl` and
`artifacts/desktop/osx-arm64/tmuxctl`. The output directories are
self-contained; the machine that runs an output does not need a separately
installed .NET runtime.

Verify that the already-running server advertises desktop protocol 1 before
opening the client:

```bash
tailscale dns status
getent ahosts YOUR-TMUXCTL-MAGICDNS-HOST
curl --fail https://YOUR-TMUXCTL-SERVER/api/desktop/capabilities
```

`tailscale dns status` must report that Tailscale DNS is enabled on the client,
and the operating-system lookup must return the current Tailscale address. A
successful `tailscale status`, Serve mapping, or curl request using `--resolve`
does not replace this check because the desktop app uses normal operating-system
DNS resolution.

The response must contain protocol version 1 and the `session-tabs-v1`,
`terminal-websocket-v1`, and `tmux-topology-v1` features. A 404 means the server
must be updated before this desktop build can connect. An HTML response also
means the deployed server predates this capability contract and must be updated.

## Ubuntu x64

On Ubuntu 24.04, install the native WebKitGTK runtime if it is absent:

```bash
sudo apt-get install libwebkit2gtk-4.1-0
./artifacts/desktop/linux-x64/tmuxctl
```

Complete these checks with disposable tmux sessions:

1. Add two labelled HTTPS server profiles, edit one, restart tmuxctl, and verify
   both remain. Confirm the profile file is owner-only with
   `stat -c '%a %n' ~/.config/tmuxctl/profiles.json`; expected mode is `600`.
2. Connect to the intended server and authenticate in the server-hosted login
   screen. Return with **Servers**, reconnect, and confirm no key is prefilled.
3. Create two disposable sessions. Open both and confirm the mobile PWA or
   another tmux client reports two additional attachments.
4. With xterm focused, use Ctrl+PageUp and Ctrl+PageDown to cycle session tabs.
   Use Ctrl+Shift+W to close one tab and confirm only that attachment disappears
   while both tmux sessions remain alive.
5. Pop the remaining session into a native window. Type independently in both
   windows, close the child, and confirm the root window and session survive.
6. Create and select a tmux window; split panes horizontally and vertically;
   select, resize, and close a non-final pane/window. Confirm another tmux client
   sees the same topology and final-pane/window close is refused.
7. Select terminal text and copy it with Ctrl+Shift+C. Paste a harmless sentinel
   with Ctrl+Shift+V; multiline paste must ask for confirmation. Right-click and
   verify the native context menu appears. Hold Ctrl while scrolling the mouse
   wheel in both directions and confirm terminal text grows and shrinks within
   bounded limits without scrolling terminal content; release Ctrl and confirm
   ordinary wheel behavior remains unchanged. Resize the window and confirm
   the active tmux client dimensions follow it.
8. Temporarily disconnect the client from its Tailscale network, reconnect, and
   then suspend/resume the workstation. Confirm the UI reports offline/reconnect
   states, creates no duplicate attachment, and the tmux sessions survive.
9. In one disposable single-pane session, type `exit`; confirm ordinary tmux
   semantics remove that session and its tab. For the other session, click its
   list kill control: a wrong confirmation must do nothing and the exact session
   name must terminate only that session.
10. Close the desktop app. Confirm every remaining disposable tmux session stays
    alive and all desktop-owned attachments disappear within the heartbeat
    bound. Verify the mobile PWA still uses its original cards, swipe behavior,
    and touch shortcut bar.

## Apple Silicon macOS

On an Apple Silicon Mac with the .NET 10 SDK available for the source build:

```bash
./scripts/build-desktop.sh osx-arm64
file artifacts/desktop/osx-arm64/tmuxctl
./artifacts/desktop/osx-arm64/tmuxctl
```

`file` must identify an arm64 Mach-O executable. Because this first cut is not
signed or notarized, run only the artifact built from the trusted local checkout
and use the operating system's explicit local-app approval if required. Prove at
minimum that the app reaches its native Servers screen, saves a profile,
completes the compatibility preflight, loads the protected desktop login, lists
sessions, attaches one disposable session, and detaches it on tab/window close
without ending the session. Repeat the copy/paste, resize, pop-out, network
reconnect, and sleep/wake checks where available.

## Report

Report only:

- the tested commit hash;
- Ubuntu or macOS version and CPU architecture;
- pass/fail for each numbered check;
- sanitized error category and step number for a failure.

Never include the login key, full profile JSON, Tailscale hostname, terminal
content, clipboard content, audit log, or screenshots containing private data.
