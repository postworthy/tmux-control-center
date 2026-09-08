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

## Linux and macOS profiles (C023)

`./scripts/verify.sh` runs shell recovery, desktop/native delivery checks, .NET
and frontend checks on both OSes. It enables all isolated Unix PTY/tmux tests;
missing native prerequisites are failures, not passing skips. The older
`TMUX_MOBILE_RUN_LINUX_INTEGRATION` opt-in remains an alias for standalone tests.
Set `TMUX_MOBILE_TEST_TMUX` for a nondefault absolute tmux executable.

Linux additionally requires setup, container watchdog and Compose/security
checks. macOS explicitly reports their exclusion; successful macOS verification
cannot replace the required Linux gate. Use Bash 4.3+ (Homebrew Bash on macOS),
.NET 10, native C compiler, tmux and installed frontend dependencies.
Actual desktop interaction and deployment acceptance are separate evidence.
