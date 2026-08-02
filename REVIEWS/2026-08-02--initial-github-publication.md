# Review Record: Initial Public GitHub Publication

Date: 2026-08-02
Review Boundary: first push to `postworthy/tmux-control-center`
Risk Class: T3
Related Proposal: `PROPOSALS/2026-08-02--initial-github-publication.md`

## Decision

Ready for the authorized initial push. SSH origin configuration is correct, the
live key remains only in the ignored environment, all tracked disclosures are
redacted, and the initial public `main` is a new parentless sanitized snapshot.
The exact detached tree passes independent secret scanning and canonical
verification.

## Evidence

- Git preflight: clean `fix/c009-tmux-backed-scrollback` worktree before review;
  no pre-existing remotes.
- GitHub metadata: repository is public, empty, and has no default branch.
- Remote: both fetch and push resolve to
  `git@github.com:postworthy/tmux-control-center.git`; `git ls-remote origin`
  succeeds and returns no refs.
- Ignore checks: `deploy/docker/.env`, `deploy/docker/state/`, root `.env`,
  certificate/private-key extensions, logs, and runtime state are ignored.
- Tracked and historical filename checks find no `.env`, private key,
  certificate, secret, or credential files.
- Full reachable-history content scan finds no PEM/OpenSSH private-key blocks,
  GitHub personal-access-token patterns, AWS access-key IDs, or Tailscale auth
  key patterns.
- Remediation: project goals, proposals, roadmap, and reviews replace the live
  key value with `[redacted test key]`; validation tests use `test-key` instead.
- Exact-tree scan: Gitleaks `v8.30.1` with current built-in default rules,
  network disabled, read-only filesystem/repository, no capabilities, and no
  privilege escalation scanned approximately 966 KB and found zero leaks.
- One initial scanner candidate was the documented weak-key test option name,
  not a credential. A line-specific `gitleaks:allow` annotation resolves it
  without disabling any rule or excluding a file.
- Exact detached-root canonical verification: 24 Core, 10 Infrastructure, and
  16 Server tests passed; two explicitly opt-in Linux tests skipped; both
  frontend test files and typecheck passed. Locked npm dependencies report zero
  known vulnerabilities.
- Root topology: the publication commit has an empty parent list. No old branch
  or tag is included in the approved push refspec.

## Findings

- Blocking findings: none.
- Advisory: tracked Tailscale IP and MagicDNS host values are not authentication
  secrets, but they are deployment identifiers. The owner should decide whether
  to retain or redact them in the public record.
- No code, runtime state, user tmux content, or Git ref has been sent to GitHub.

## Rollback

Before publication, remove `origin` to undo remote preparation. The local
pre-publication history is preserved only in a permission-restricted ignored
bundle and must never be pushed.

## Approval

- SSH origin configuration: approved by repository owner, 2026-08-02.
- Sanitized-history publication: authorized by repository owner, 2026-08-02.
- First push of only the clean root as `main`: approved.
