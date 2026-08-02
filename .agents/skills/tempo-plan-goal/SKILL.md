---
name: tempo-plan-goal
description: Convert an approved product or change outcome into a self-contained, resumable Tempo living goal with acceptance evidence, an authority envelope, ordered work units, retry bounds, pause conditions, and one next action. Use when the user asks to create, plan, decompose, or revise a long-running goal or execution plan; do not use merely to execute an already-active goal.
---

# Tempo Plan Goal

Create durable execution state that a fresh compatible agent can resume without
conversation history.

## Workflow

1. Read `CONSTITUTION.md`, `SPEC.md`, `GOALS/README.md`, `GOALS/TEMPLATE.md`, and
   the approved proposal or outcome.
2. Confirm that product intent and required approvals exist. Draft the missing
   higher-precedence contract first when they do not.
3. State one observable outcome and explicit non-goals.
4. Translate success into criterion IDs with observable evidence sources.
5. Classify risk and record an Authority Envelope:
   - allow approved, local, reversible T0/T1 work,
   - pause for scope expansion, unapproved T2/T3, destructive, external,
     production, compatibility, or unclear security/privacy actions.
6. Decompose the outcome into small ordered units with exit criteria and
   verification. Name the thin slice.
7. Set a bounded retry policy, pause conditions, review boundary, and exactly one
   immediately executable next action.
8. Ensure at most one goal is active; use `draft` or `approved` until execution
   should begin.
9. Run the repository's goal validator (`pnpm check:goal` in Tempo's default
   starter profile) and resolve every structural error.

Read [goal-quality.md](references/goal-quality.md) when auditing a complex goal or
deciding whether evidence and decomposition are strong enough.

## Completion

Finish only when the goal is self-contained, validation passes, every work unit
has an exit condition, authority is unambiguous, and a fresh reader can identify
the next action.
