# Portable Tempo Installation

This directory is installed by Tempo's target-based adopt-existing mode. It
contains governance and workflow only; it does not choose or install the target
application stack.

Owned paths are listed in `install-manifest.txt`. A pre-existing root
`AGENTS.md` is backed up before Tempo appends its marked routing block.

Use templates under `.tempo/templates/` to create project-owned contracts.
Repo-local skills live under `.agents/skills/`, and living goals live under
`GOALS/`.

To roll back, first obtain approval for file removal, consult the manifest,
restore the AGENTS backup when present, and remove only Tempo-created paths.
