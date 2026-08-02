# Proposal: <title>

Date: YYYY-MM-DD
Owner: <human/agent name>
Risk Class: T0 / T1 / T2 / T3
Related Issue/Context: <optional>
Roadmap Item: Cxxx
Planned Branch: <type>/cxxx-<short-name>
Expected Commit Count: <n>

## Objective

Describe the single outcome this change should produce.

## Scope

In scope:

- <item 1>
- <item 2>

Out of scope:

- <item 1>
- <item 2>

## Expected Files Touched

- `path/to/file1`
- `path/to/file2`

## Acceptance Criteria

- [ ] <observable result 1>
- [ ] <observable result 2>
- [ ] <observable result 3>

## Verification Plan

Commands:

```bash
pnpm verify
```

Optional focused checks:

```bash
# example
pnpm --filter <pkg> test
```

Pass means:

- Canonical verification exits 0.
- Acceptance criteria are demonstrably satisfied.

## Change Review Plan

- Review Boundary: merge from `<feature-branch>` into `main`
- Planned Review Record: `REVIEWS/YYYY-MM-DD--short-title.md`
- Reviewer/approver expectation: <name/role or criteria>

## Git Plan

- Branch command: `git switch -c <type>/cxxx-<short-name>`
- Commit subject pattern: `<type>(<scope>): <summary>`
- Required commit trailers:
  - `Roadmap: ROADMAP/COMMIT-PLAN.md#Cxxx`
  - `Proposal: PROPOSALS/YYYY-MM-DD--short-title.md` (or `Proposal: N/A (T0)` where approved)
- Planned merge method: `git merge --no-ff <feature-branch>`

## Decomposition Plan (Required for T1/T2/T3)

Work units (ordered):

1. <unit> — Verify by: <command/evidence> — Exit criteria: <definition> — Risk: <T0/T1/T2/T3> — Dependencies: <if any>
2. <unit> — Verify by: <command/evidence> — Exit criteria: <definition> — Risk: <T0/T1/T2/T3> — Dependencies: <if any>

Thin slice milestone:

- <what minimal end-to-end capability exists after unit N>

Dependencies and unknowns:

- <dependency or unknown 1>
- <dependency or unknown 2>

Intentional deferrals:

- <deferred item 1>
- <deferred item 2>

## Rollback Plan

If this change causes regressions:

1. Revert commit(s): <how>
2. Restore prior behavior: <how>
3. Validate rollback with: <command(s)>

## Risks and Mitigations

- Risk: <risk 1>
  Mitigation: <mitigation 1>
- Risk: <risk 2>
  Mitigation: <mitigation 2>

## Compatibility / Migration Notes

- API compatibility impact: <none / describe>
- Data/schema migration needed: <no / yes + plan>
- Backward compatibility window: <if applicable>

## Observability / Debug Notes (if relevant)

- New logs/metrics/traces: <list>
- How to detect failure quickly: <signals/alerts>

## Approval

- Requested from: <name/role>
- Approval status: <pending/approved>
- Approved at: <timestamp>
