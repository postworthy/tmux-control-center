# tmuxctl

View and use your tmux sessions from your phone or desktop. Check on running
work, open a terminal when you need it, and manage sessions from one interface.
[tmux](https://github.com/tmux/tmux) keeps terminal sessions running after you
disconnect, so you can return to the same work later.

Run the tmuxctl server on the Linux or Mac computer where your tmux sessions
live. Connect to it through the mobile browser app or the desktop client.

## What you can do

- See your sessions, preview their output, and filter by name or attachment state.
- Open a live terminal with mobile touch controls or a desktop keyboard and mouse.
- Create, rename, and explicitly terminate sessions. Closing a desktop tab simply
  detaches; killing a session requires confirmation and ends that session.
- Keep several sessions open in desktop tabs or split views, and switch between
  saved servers.

Session status is a best-effort hint based on terminal activity; ambiguous states
show as **Unknown**. Optional workspace recovery can save session layouts and
working directories for an explicit restore after reboot.

## Get started

**Already have a server?** Open its HTTPS address and sign in with the login key
supplied by its owner. On iPhone, open it in Safari and add it to your Home Screen
for an app-like experience. For a desktop window, follow the
[desktop build and usage guide](docs/desktop.md).

**Setting up your own server?** Start with the guide for the computer running tmux:

- **Linux:** [Docker Compose setup](deploy/docker/README.md) covers prerequisites,
  configuration, and starting the server. [Native Linux deployment](docs/deployment.md)
  covers systemd and other HTTPS options.
- **Apple Silicon Mac:** [Native macOS setup](docs/macos-server.md) covers building,
  configuration, and optional launchd supervision.

Run the server as the same non-root user that owns your tmux sessions. After
setup, open its HTTPS address and sign in. The desktop client needs an
already-running server; it does not install or start one.

Keep access private. The setup guides cover authentication, HTTPS, and Tailscale;
retain the login requirement even on your private network.

## Platforms and availability

The server and desktop target **Ubuntu Linux x64** and **Apple Silicon macOS**.
The mobile browser app is designed for iPhone. Desktop builds currently come from
source; packaged installers, signing, and published binary releases are deferred.
Intel Macs and Windows are outside the current target platforms.

Each server manages one local tmux host with one owner identity. Saved server
switching connects to independently configured servers, each with its own login.
Physical-device acceptance remains a separate step; see the
[desktop checklist](docs/desktop-acceptance.md) and [project status](STATUS.md).

## Documentation and development

- [Desktop guide](docs/desktop.md): build, connect, tabs, shortcuts, and clipboard.
- [Development guide](docs/development.md): local setup, hot reload, builds, and tests.
- [Configuration reference](docs/configuration.md) and [security guide](docs/security.md).
- [Architecture](docs/architecture.md) and [HTTP/WebSocket API](docs/api.md).

Contributing? Start with the development guide, then read the [project contract](SPEC.md)
and [repository instructions](AGENTS.md).
