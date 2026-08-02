---
name: tempo-perform-rca
description: Investigate and document a Tempo root-cause analysis after a requested change or fix failed, regressed, or repeatedly produced the wrong result. Use when the user says a fix did not work, the same failure returned, observed output contradicts the implementation claim, or another corrective attempt requires evidence first; do not use for an ordinary first-pass bug diagnosis with no prior failed change.
---

# Tempo Perform RCA

Establish why the prior request and implementation diverged before attempting
another fix.

## Workflow

1. Stop corrective implementation and preserve the failure evidence.
2. Read the original request, approved scope, implementation diff, verification
   claim, and observed output.
3. Reproduce the symptom with the smallest reliable procedure.
4. Separate facts from hypotheses and collect evidence that can falsify each
   likely cause.
5. Identify the root cause at the process, contract, implementation, or
   verification layer. Do not stop at the visible symptom.
6. Create `RCA/YYYY-MM-DD--short-title.md` from `RCA/TEMPLATE.md` with symptom,
   reproduction, evidence, root cause, corrective action, and preventive control.
7. Define a narrower corrective proposal or next action and its regression test.
8. Update the active goal, `STATUS.md`, and `DECISIONS.md` when their owned state
   changes.
9. Resume implementation only after the RCA supports a corrective action within
   current authority.

Read [causal-analysis.md](references/causal-analysis.md) when reproduction is
intermittent, several causes are plausible, or the earlier verification passed.

## Completion

Finish only when reproduction is documented, root cause is supported by
evidence, the original verification gap is explained, and the preventive control
would catch the same class of failure.
