# Review Record: README cleanup

Date: 2026-09-08
Review Boundary: merge from `docs/c024-readme-cleanup` into `main`
Merge Method: `git merge --no-ff docs/c024-readme-cleanup`
Risk Class: T0 (documentation only)
Related Proposal: `PROPOSALS/2026-09-08--readme-cleanup.md`
Decision: ready

## Scope and Git conformance

Implementation commit: `6ad7f7a` — `docs(readme): simplify onboarding and extract focused guides`.
Baseline: `dd23d658af251484a4ec69408d8afea43b516331`, confirmed equal to local
and fetched origin/main before merge. A second documentation commit records this
review. Both commits use conventional subjects and Roadmap/Proposal trailers;
all edits are on the feature branch, with no direct commit to main.

The diff contains the README, desktop/development guides, small cross-links and
platform corrections in existing guides, and proposal/roadmap/decision records.
No runtime code, dependencies, configuration defaults, generated assets, secrets,
services, or existing living goals changed.

## Acceptance evidence

- README is 466 whitespace-delimited words, down from 1,634 (about 71% shorter).
  Its opening explains the server/client relationship before prerequisites.
- Existing-server readers get HTTPS/login and phone/desktop routes; operators get
  separate Linux Compose, native Linux, and native Mac links. Compose preparation
  now names prerequisites and gives a clone command; Mac links to clone setup.
- Ubuntu x64 and Apple Silicon support, source-only desktop availability,
  existing-server requirement, one owner/host per server, and private access remain
  explicit. The README distinguishes detach from confirmed session termination.
- Compared the old README against the destination guides: build, launch, chooser,
  layouts, shortcuts, clipboard bounds, local development, hot reload, publication,
  and test commands are preserved. Existing architecture/security guides retain
  the implementation controls and recovery limits. Speculative roadmap text was
  removed from the landing page.
- Corrected stale Linux-only server/PTY claims and the claim that canonical Unix
  tests are opt-in. Pending physical-device acceptance remains visible.
- Checked 41 local Markdown links and anchors across the 11 changed/new documents;
  all resolved. The review record itself adds no Markdown links.

## Verification

Commands:

```bash
PATH="$PWD/.dotnet:$PATH" ./scripts/verify.sh > /tmp/c024-verify.log 2>&1
python3 /tmp/c024-check-links.py
git diff --check
```

Canonical gate exited 0 on Linux. All 166 .NET tests passed (32 Core, 42 Desktop,
34 Infrastructure, 58 Server), with zero skips. All 15 frontend test files passed;
frontend type checking, shell recovery, setup/watchdog, desktop/native delivery,
and Linux Compose/security checks passed. Link/anchor checks and whitespace checks
passed. Verification scripts and application code were unchanged. The subsequent
Compose prerequisite paragraph and this review are documentation-only additions.
No services were installed or deployed, and no physical-device result is inferred
from automated checks. Existing C022/C023 acceptance follow-ups remain separate.

## Risk, rollback, and approval

No blocking findings. Risk is limited to documentation accuracy; route, command,
platform, and moved-content review covers the approved scope. No compatibility or
data migration is involved. Roll back by reverting the two documentation commits
on a feature branch and reviewing/merging that revert; preserve runtime state.

Reviewer: Codex, applying the repo-local Tempo review skill.
Owner authorization: Landon's 2026-09-08 instruction explicitly approves making
these changes, committing, merging with main, and pushing upstream; recorded in
D014. Publish to the existing origin using a normal merge and push. No force push
or production changes are authorized by this documentation task.

Follow-ups for C024: none.
