# Proposal: Make the README welcoming to new users

Date: 2026-09-08
Status: Approved — implemented; verification recorded in the C024 review
Owner: Landon
Risk Class: T0 (documentation only)
Roadmap Item: C024
Planned Branch: `docs/c024-readme-cleanup`
Expected Commit Count: 1–2
Baseline: `dd23d65`

## Objective

A new reader should understand what tmuxctl does, whether it fits their setup,
and where to start within the first screen. Target roughly 400–550 words,
down from the current 1,634 words across 236 lines. Treat the README as the
entry point; put detailed instructions in focused guides.

## What makes the current README difficult

- The introduction leads into deployment internals before explaining the basic
  relationship: a server runs beside tmux; a phone or desktop connects to it.
- “What is included” mixes user benefits with implementation details such as
  opaque identifiers, CSRF policies, forkpty, and WebSocket attachment lifetimes.
  One desktop feature bullet alone spans more than twenty lines.
- “Development” is the first getting-started path. A person who wants to use the
  app must distinguish development mode, production builds, desktop builds and
  Docker deployment without guidance.
- Shortcuts, operational instructions, architecture and acceptance status are
  interleaved. Some instructions are repeated in the supporting documentation.
- Several statements are stale: the introduction and limitations say Linux-only
  server/PTY; verification describes tests as Linux-only and opt-in although the
  canonical gate now runs Unix integration tests. Blanket statements that
  destructive operations or multiple hosts are absent obscure confirmed session
  termination and switching between independently configured servers.

## Proposed README structure

| Section | What the reader gets |
| --- | --- |
| Introduction | Two or three sentences explaining the product and server/client relationship |
| What you can do | Four short benefits: view sessions, interact with terminals, manage sessions, connect from phone/desktop |
| Get started | An existing-server path, followed by Linux and macOS server setup links; then connect and sign in |
| Platforms and availability | Ubuntu x64 and Apple Silicon macOS; mobile browser app; source-built desktop; one tmux host per server |
| Documentation and development | A compact guide list and one contributor entry point |

Keep a short security note beside setup: self-hosted, private access through
Tailscale/HTTPS, login required. Keep the useful behavioral distinction that
closing a desktop tab detaches while explicitly killing a session terminates it.
Do not turn either point into an architecture or security checklist.

## Example opening and start flow

The following is illustrative wording, not a replacement already applied:

> # tmuxctl
>
> View and use your tmux sessions from your phone or desktop. Check on running
> work, open a terminal when you need it, and manage sessions from one interface.
>
> Run the tmuxctl server on the Linux or Mac computer where your tmux sessions
> live. Connect to it through the mobile browser app or the desktop client.
>
> ## Get started
>
> **Already have a tmuxctl server?** Open its HTTPS address and sign in with
> the login key supplied by its owner. On iPhone, you can add it to your Home
> Screen. For the desktop client, follow the desktop guide.
>
> **Setting up your own server?** Choose the Linux setup guide for Docker
> Compose, or the native macOS server guide. Once setup is complete, open the
> server's HTTPS address and sign in.
>
> tmuxctl is self-hosted and intended for private access. The setup guides walk
> through authentication, HTTPS and Tailscale.

In the finished README, “desktop guide,” “Linux setup guide” and “native macOS
server guide” become links. Avoid a pretend one-command quick start: the required
host, tmux and authentication setup belongs in those guides.

## Where the existing material goes

| Current material | Destination |
| --- | --- |
| Detailed session tabs, layouts, shortcuts, clipboard and server chooser behavior | New `docs/desktop.md`: user-facing build/connect/use guide |
| SDK prerequisites, local development, hot reload, build/test commands | New `docs/development.md` |
| Docker, tmux compatibility, environment/key preparation, guided agent setup | Existing `deploy/docker/README.md` |
| Native Mac server publish/configuration and launchd | Existing `docs/macos-server.md` |
| Linux native/systemd, TLS alternatives, recovery, upgrades and rollback | Existing `docs/deployment.md` |
| PTY/WebSocket internals and profile/capability mechanics | Existing `docs/architecture.md` |
| Authentication implementation, file permissions and encryption limits | Existing `docs/security.md` and `docs/configuration.md` |
| Device walkthroughs and remaining physical acceptance | Existing `docs/desktop-acceptance.md`, `STATUS.md` and current review records |
| Speculative “recommended next steps” | Remove from README; roadmap already owns future work |

Reuse existing explanations instead of duplicating them. The desktop acceptance
checklist should stay a validation checklist, not become the newcomer tutorial.
Correct platform-specific introductions in destination guides only where needed
so following a README link does not immediately contradict the new support summary.
Clearly label any guide that applies only to Linux.

## Scope and implementation sequence

1. Move unique operational/development content into the two focused guides;
   reconcile overlap with existing docs and add links back to the README.
2. Rewrite README around the structure above, simplify language and correct
   stale platform/test/feature claims against current source and verification.
3. Check each newcomer route end to end: existing server, first Linux server,
   first Mac server, and source-built desktop. Review links and retained details.

Expected files: README, the two new guides, and small introduction/cross-link
edits in existing deployment, Mac setup, architecture or configuration docs.
No application, dependency, deployment-default, verification-command or feature
changes. No new installation promises, fabricated screenshots, or claims that
pending physical acceptance has been completed. Existing goals remain unchanged.

## Acceptance and verification

- [x] README is approximately 400–550 words and explains the server/client
  relationship before presenting technical requirements.
- [x] A newcomer can choose a setup path without knowing ASP.NET, Photino,
  xterm.js, Tempo, or the repository's internal structure.
- [x] Linux and Mac server paths are explicit; the desktop requires an existing
  server; source-build availability and one-host-per-server scope remain clear.
- [x] Unique instructions are preserved in linked guides; detailed shortcuts,
  configuration values and implementation mechanisms leave the landing page.
- [x] All README links resolve, and setup guides supply prerequisites and a
  concrete first action. No uncreated guide is linked in the shipped README.
- [x] Authentication and private-network expectations stay visible; user-facing
  claims match current code without erasing pending acceptance work.

For implementation, inspect local Markdown links/anchors, compare moved material,
and run `./scripts/verify.sh` with the repository's .NET 10 SDK on PATH. Do not
install services or run deployment commands as part of documentation verification.
Results are recorded in `REVIEWS/2026-09-08--readme-cleanup.md`.

## Review, Git and rollback

Review the before/after README and each destination guide as a documentation-only
change. Use conventional `docs(readme)` / `docs(guides)` commits with:

```text
Roadmap: ROADMAP/COMMIT-PLAN.md#C024
Proposal: PROPOSALS/2026-09-08--readme-cleanup.md
```

Revert the documentation commits to roll back; no runtime or data migration is
involved. Main merge and publication were explicitly authorized by the owner.

Approval: Landon approved implementation, commits, merge into main, and push to
the existing upstream on 2026-09-08: “do it, make the changes, commit them and
merge with main and push upstream”.
