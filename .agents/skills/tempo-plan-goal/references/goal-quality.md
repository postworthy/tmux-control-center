# Goal Quality Checklist

Use this checklist for a goal that spans multiple commits, sessions, or risk
boundaries.

## Outcome and Evidence

- Describe the end state independently of the proposed implementation.
- Give every criterion a stable ID and an observable evidence source.
- Do not use “code exists,” “tests added,” or “looks correct” as sole product
  evidence.
- Keep final integration, migration, and clean-environment behavior explicit.

## Decomposition

- Make each unit independently reviewable and verifiable.
- Put dependency-enabling work before consumers.
- Identify one thin end-to-end slice early.
- Separate high-risk policy or compatibility changes from routine implementation.
- Record deliberate deferrals as non-goals rather than letting them disappear.

## Resumption

- Keep completed work immutable unless evidence invalidates it.
- Record discoveries that change assumptions.
- Point to commands or artifacts rather than summarizing success vaguely.
- Maintain exactly one next action; rewrite it after every checkpoint.
- Treat host-native loop state as an adapter, never as the only durable state.

## Completion

- Require final evidence for every criterion.
- Require canonical verification and documentation agreement.
- Reject completion if follow-up work is required to make the outcome usable.
- Change status to `completed` only after the repository validator passes.
