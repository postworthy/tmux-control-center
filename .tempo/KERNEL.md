# Tempo Portable Repository Kernel

This repository is the source of truth. This portable kernel adds Tempo workflow
without choosing or replacing the application's language, framework, package
manager, or verification tool.

## Governing Order

1. `.tempo/CONSTITUTION.md`
2. project `SPEC.md`
3. `.tempo/VERIFY.md`
4. project `DECISIONS.md`
5. project roadmap
6. approved project proposals
7. one active `GOALS/*`
8. reviews and RCA records
9. human-facing status

No lower-precedence artifact may weaken a higher one.

## Invariants

- Read the complete command in `.tempo/VERIFY_COMMAND` as the target's canonical
  verification command.
- Preserve the target repository's application stack and unrelated user work.
- Work on a feature branch; never commit directly to the target's primary branch.
- Non-trivial work requires approved scope, risk, decomposition, evidence,
  rollback, documentation, and review.
- Never commit secrets, generated artifacts, caches, or local logs.
- Never perform destructive/irreversible, remote/publication, production,
  compatibility-breaking, or unclear security/privacy actions without explicit
  approval.
- Perform RCA before another attempt after a user reports that a fix failed.

## Route Conditional Work

Use the repo-local skill matching the task:

- `tempo-onboard-project`: discover greenfield or existing product intent.
- `tempo-plan-goal`: create or revise a resumable living goal.
- `tempo-execute-goal`: continue the active goal.
- `tempo-review-change`: assess review-boundary readiness.
- `tempo-perform-rca`: investigate a failed prior change.

Templates are under `.tempo/templates/`. Living-goal lifecycle guidance is under
`GOALS/`.

## Preflight

Before edits:

```bash
git rev-parse --abbrev-ref HEAD
git status --short
```

If the branch is the primary branch, create a feature branch. If unrelated dirty
files overlap the task, pause and ask. Confirm the smallest approved work unit
and its evidence before implementation.

## Authority and Loop

Approved, local, reversible T0/T1 work inside a recorded Authority Envelope may
continue without repeated confirmation. Pause at every boundary listed above or
in the active goal.

For each work unit:

1. Orient from repository and active-goal state.
2. Confirm scope, risk, authority, exit condition, and rollback.
3. Implement the smallest coherent reversible change.
4. Observe actual behavior.
5. Run focused checks and the recorded canonical verification command.
6. Checkpoint evidence, progress, discoveries, decisions, retries, and one next
   action.
7. Continue until a real pause condition or evidence-backed completion.

## Completion

Do not claim completion until every criterion has final evidence, canonical
verification passes, docs match behavior, rollback is viable, required review
exists, and no required work remains.
