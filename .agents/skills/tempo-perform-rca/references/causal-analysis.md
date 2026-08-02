# Causal Analysis Guide

## Compare Four States

1. Requested outcome and acceptance criteria.
2. Approved implementation approach.
3. Actual code or configuration delivered.
4. Observed runtime result.

Locate the earliest divergence. A later exception message may be only the
downstream symptom.

## Evidence Practices

- Preserve exact reproduction inputs and outputs.
- Change one variable at a time.
- Prefer minimal reproductions over broad reruns.
- Check environment, versions, state, and test doubles when verification passed
  but reality failed.
- Name uncertainty; do not present correlation as cause.

## Root-Cause Quality

A useful root cause explains:

- why the symptom occurred,
- why the prior implementation allowed it,
- why verification failed to detect it,
- and what control prevents recurrence.

“Bad input,” “coding error,” and “test gap” are categories, not sufficient root
causes.

## Corrective Action

- Fix the earliest controllable cause.
- Add a regression check at the missing boundary.
- Avoid expanding scope to unrelated cleanup.
- Verify both the original reproduction and the canonical gate.
