---
name: tempo-onboard-project
description: Guide Tempo discovery for a new idea or an existing repository and turn user intent into an approved PROJECT-BRIEF.md and SPEC.md before implementation. Use when starting a project with Tempo, adopting Tempo into an existing codebase, clarifying a vague product idea, choosing greenfield versus adopt-existing onboarding, or revising incomplete project scope.
---

# Tempo Onboard Project

Turn plain-language intent and grounded repository evidence into approved product
contracts. Keep discovery separate from implementation.

## Workflow

1. Read `AGENTS.md`, `CONSTITUTION.md`, `BOOTSTRAP.md`, and the current
   `PROJECT-BRIEF.md` and `SPEC.md`.
2. Determine `greenfield` or `adopt-existing` mode. For adopt-existing, run the
   documented repository intake scan and label findings as hypotheses.
3. Reflect the user's desired outcome in plain language.
4. Ask at least three high-value questions about users, observable v1 outcomes,
   non-goals, constraints, and delivery shape. Ask a follow-up for every
   materially ambiguous answer.
5. Offer two or three viable v1 scopes with concrete trade-offs and recommend the
   smallest complete vertical slice.
6. Confirm the direction before editing product contracts.
7. Draft `PROJECT-BRIEF.md`, then `SPEC.md`, preserving the required sections and
   distinguishing confirmed facts from inferred hypotheses.
8. Run contract validation. Present unresolved assumptions and stop for explicit
   approval before non-trivial implementation.

Read [intake-quality.md](references/intake-quality.md) when answers remain
ambiguous or adopt-existing evidence conflicts with stated intent.

## Guardrails

- Do not infer core product requirements merely because the repository suggests
  them.
- Do not install an application stack during adopt-existing discovery.
- Do not begin feature implementation while the brief or specification is
  unapproved.
- Keep novice-facing questions in plain language; translate answers into
  engineering constraints without requiring the user to learn the jargon.

## Completion

Finish only when the onboarding mode is recorded, placeholders are resolved,
product acceptance criteria are observable, non-goals and risks are explicit,
contract validation passes, and the user has approved the direction.
