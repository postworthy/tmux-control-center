# Review Record: <short-title>

Date: YYYY-MM-DD
Review Boundary: merge from `<feature-branch>` into `main`
Merge Method: `git merge --no-ff <feature-branch>`
Risk Class: T0 / T1 / T2 / T3
Related Proposal(s): <path(s)>

## Branch

- Source branch: `<feature-branch>`
- Target branch: `main`

## Commits in Scope

- <commit hash> <summary>
- <commit hash> <summary>

## Git Conformance Checklist

- [ ] Source branch matches naming policy
- [ ] No direct commit to `main`
- [ ] Commit subjects are conventional
- [ ] Commit trailers include `Roadmap` and `Proposal`
- [ ] Commits in scope match approved proposal/decomposition

## Change Summary

- <what changed>
- <why it changed>

## Acceptance Checklist

- [ ] Scope matches approved proposal/decomposition
- [ ] Acceptance criteria are satisfied
- [ ] Docs updated where required
- [ ] Rollback path is clear

## Verification Evidence

Commands run:

```bash
pnpm verify
```

Results:

- <pass/fail summary>
- <notable details>

## Rollback Plan

1. <rollback step 1>
2. <rollback step 2>

## Approvals

- Reviewer: <name/role>
- Approval status: <approved/rejected/pending>
- Timestamp: <YYYY-MM-DD HH:MM TZ>

## Follow-Ups

- <follow-up item 1 or none>
- <follow-up item 2 or none>
