# Living Goals

A living goal is Tempo's canonical active execution state. It records an
approved outcome, criterion evidence, authority, progress, retries, and one next
action so work can resume without conversation history.

## Lifecycle

- `draft`: being prepared.
- `approved`: ready but not started.
- `active`: the one goal currently executing.
- `paused`: waiting at a defined boundary.
- `blocked`: repeated evidence proves meaningful progress requires a state
  change.
- `completed`: every criterion and completion gate has evidence.
- `abandoned`: intentionally ended without the outcome.

At most one goal may be `active`.

## Execution Loop

1. Orient from repository and goal state.
2. Select the recorded next action or smallest unmet criterion.
3. Check authority and risk.
4. Act through a small reversible unit.
5. Observe actual behavior.
6. Evaluate evidence and repair within the retry bound.
7. Checkpoint all state and one next action.
8. Continue until a defined pause or evidence-backed completion.

Use `GOALS/TEMPLATE.md`. Run the goal validator available in the target
repository when one exists; otherwise inspect every required section and ensure
there is no second active goal before execution.
