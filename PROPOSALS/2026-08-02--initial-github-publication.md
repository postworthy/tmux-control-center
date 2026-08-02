# Proposal: Initial Public GitHub Publication

Date: 2026-08-02
Owner: Human Partner and AI Agent
Risk Class: T3
Target: `git@github.com:postworthy/tmux-control-center.git`

## Objective

Configure the empty public GitHub repository as the SSH origin and publish only
after the repository and its complete reachable history are demonstrably free
of usable credentials and unintended private deployment material.

## Scope

In scope:

- Configure and verify the `origin` SSH remote without embedding credentials.
- Audit tracked files, generated assets, paths, and complete reachable history.
- Remove every live-credential disclosure from the publication snapshot and
  ensure pre-sanitization commits are unreachable from every pushed ref.
- Run canonical verification and record a publication Change Review.
- Push only the explicitly approved refs after all gates pass.

Out of scope:

- Changing repository visibility, GitHub settings, branch protections, Actions,
  releases, packages, or deployment infrastructure.
- Publishing local `.env`, certificates, runtime state, logs, captured terminal
  content, or user tmux data.
- Rewriting or deleting local history without separate explicit approval.

## Acceptance Criteria

- [x] `origin` uses `git@github.com:postworthy/tmux-control-center.git` for fetch
  and push, and SSH read access succeeds.
- [x] The target repository is confirmed public and empty before first push.
- [x] Local runtime `.env`, state, secrets, private keys, and certificates are
  ignored and absent from tracked paths and historical filenames.
- [x] History scan finds no private-key blocks, GitHub tokens, AWS access keys,
  or Tailscale auth keys.
- [x] The tracked disclosure is redacted without changing the ignored live key;
  validation tests use a distinct non-secret fixture.
- [x] The owner selects a new single-root `main`; no pre-sanitization branch or
  tag will be pushed.
- [x] The new root commit has no parent and passes a scan of its exact tree.
- [x] Canonical verification and final publication review pass immediately
  before the first push.
- [x] The owner explicitly authorizes the history sanitization needed to prevent
  any secret from being published.

## Abort and Rollback

- Abort before push on any unresolved credential, private runtime file, unclear
  ref scope, failed verification, or remote mismatch.
- Before the first push, rollback is `git remote remove origin`; no remote data
  exists to repair.
- After publication, any discovered credential must be revoked immediately and
  the remote history treated as compromised.

## Approval

- SSH remote configuration: approved by repository owner on 2026-08-02.
- Sanitized single-root `main` and first push: approved by the owner's direction
  to do whatever is necessary to keep secrets out of Git history.
