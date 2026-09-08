#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash -n "$repository_root/scripts/build-server.sh"
bash -n "$repository_root/scripts/workspace-platform.sh"
bash -n "$repository_root/scripts/tmux-workspace-recovery.sh"
# Parse the templates structurally on both platforms; actual launchd loading is
# an operator action tested only in a separately approved service environment.
if [[ $(uname -s) == Darwin ]]; then
  plutil -lint "$repository_root/deploy/launchd/com.tmuxctl.server.plist.example"
  plutil -lint "$repository_root/deploy/launchd/com.tmuxctl.workspace.plist.example"
else
  python3 - "$repository_root" <<'PY'
import plistlib,sys
from pathlib import Path
for p in (Path(sys.argv[1])/'deploy/launchd').glob('*.plist.example'):
    data=plistlib.loads(p.read_bytes())
    assert data['ProgramArguments'][0].startswith('/')
    assert data['AbandonProcessGroup'] is True
    assert data['Umask'] == 0o077
    assert 'HOME' not in data.get('EnvironmentVariables',{})
PY
fi
echo "native delivery checks passed"
