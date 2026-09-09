# Expired authentication recovery

Status: approved (user request, 2026-09-09; option A)
Risk: T1 — local client recovery using existing authentication boundary.

Outcome: Desktop and PWA explicitly request the access key on the current server
when protected HTTP requests return 401, including session creation. A successful
login clears cached CSRF and permits the user to retry. Live inventory cannot
hide the prompt. Resume and inventory disconnect check HTTP authentication.

Non-goals: changing cookie lifetime, storing passwords, automatic mutation replay,
deployment, publication, or server-side authentication changes.

Units: (1) shared authentication notification and client prompts; (2) regression
checks, canonical verification, documentation and review. Verify rejected create,
failed login, successful login/new CSRF/retry, and non-auth errors for both APIs.
Rollback: revert only this proposal's source and documentation changes.
