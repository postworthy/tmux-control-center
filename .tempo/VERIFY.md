# Tempo Portable Verification Contract

The target repository's canonical verification command is stored as the complete
first line of:

```text
.tempo/VERIFY_COMMAND
```

Run that command from the target repository root after focused checks and before
review. Tempo stores the user-provided command as data during installation; the
installer does not execute it.

Record:

- the exact command,
- pass or fail,
- relevant output,
- and failure interpretation or next action.

If the command becomes stale, update it through an approved change and keep this
contract, the repository kernel, and public setup guidance aligned.
