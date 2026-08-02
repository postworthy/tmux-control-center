---
name: tempo-review-change
description: Perform a Tempo Change Review for a feature branch or completed work unit by comparing approved scope, implementation, criterion evidence, verification, risk, compatibility, rollback, documentation, and git history. Use when asked to review, audit, assess merge readiness, create a Review Record, or decide whether work may cross the local Review Boundary; do not use as a request to implement the change.
---

# Tempo Review Change

Produce an evidence-backed readiness decision without silently repairing the
change under review.

## Workflow

1. Read the governing contracts, approved proposal, active goal, roadmap item,
   and `REVIEWS/TEMPLATE.md`.
2. Run git preflight and identify the exact commits and diff in scope.
3. Map every acceptance criterion to implementation and observable evidence.
4. Inspect focused tests before treating their green status as proof.
5. Run canonical verification and record exact results.
6. Review risk, safety, compatibility, migration, rollback, documentation,
   generated artifacts, secrets, and unrelated changes.
7. Classify findings by severity and cite file/line or command evidence.
8. Create or update the Review Record with the decision:
   - ready,
   - ready with explicit follow-ups,
   - or not ready.
9. Do not merge, push, publish, or fix findings unless separately authorized.

Read [review-checklist.md](references/review-checklist.md) for the complete audit
surface on T2/T3, migration, or release-boundary reviews.

## Completion

Finish only when scope and commits are exact, all criteria have evidence,
verification results are current, rollback is credible, findings are actionable,
and the Review Record states who approved the boundary.
