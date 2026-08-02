# Loop and Recovery Rules

## Evidence Hierarchy

Prefer, in order:

1. observed runtime or environment behavior,
2. focused tests that cover the exact requirement,
3. canonical verification,
4. source inspection,
5. narrative status.

A narrow test cannot prove a broader acceptance criterion.

## Retry Discipline

- Record the failure and likely cause before retrying.
- Retry only while the cause or intervention changes.
- Increment the goal's attempt count for the same unchanged failure.
- Respect its maximum bound.
- If the user reports that a requested fix failed, stop and use
  `tempo-perform-rca` before another corrective attempt.

## State Decisions

- `active`: meaningful in-scope progress remains possible.
- `paused`: a defined approval, time, or external boundary is expected.
- `blocked`: the same condition repeatedly prevents meaningful progress and the
  repository's block threshold is satisfied.
- `completed`: every criterion has final evidence and no required work remains.
- `abandoned`: the owner intentionally ends the goal without the outcome.

## Context Handoff

Before yielding:

- update the goal from observed state,
- preserve partial evidence and failures,
- name one exact next action,
- ensure validation passes unless the failure itself is the recorded blocker,
- and avoid relying on conversation-only instructions.
