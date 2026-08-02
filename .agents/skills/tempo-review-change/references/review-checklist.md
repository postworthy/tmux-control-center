# Change Review Checklist

## Scope and History

- Proposal is approved and implementation stays in scope.
- Commits are atomic, conventional, and carry required trailers.
- Diff contains no unrelated user work, logs, caches, secrets, or generated
  evaluation workspaces.

## Behavior and Evidence

- Each criterion maps to relevant implementation and an observable result.
- Tests exercise the requirement's full scope and meaningful negative paths.
- Canonical verification is current and reproducible.
- Manual evaluation is present where subjective or agent behavior matters.

## Risk and Recovery

- Risk classification still matches discovered impact.
- Security/privacy, destructive, external, production, and compatibility
  boundaries were respected.
- Migration and backward compatibility are explicit where applicable.
- Rollback restores a coherent prior state and has a validation command.

## Documentation and Boundary

- User-facing and agent-facing documentation match current behavior.
- Goal, roadmap, decisions, status, and proposal do not conflict.
- Review Record names commits, evidence, findings, follow-ups, approver, and
  boundary decision.
- No merge or external action occurs as an implied consequence of review.
