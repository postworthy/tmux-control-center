interface PaneTopology {
  id: string;
  isActive: boolean;
}

interface WindowTopology {
  isActive: boolean;
  panes: PaneTopology[];
}

interface SessionTopology {
  sessionId: string;
  windows: WindowTopology[];
}

interface NamedDesktopTab {
  sessionId: string;
  name: string;
}

interface NamedSession {
  id: string;
  name: string;
}

export function sessionIconLabel(name: string): string {
  const words = name.trim().split(/[\s._-]+/u).filter(Boolean);
  if (!words.length) return "?";
  if (words.length > 1) return `${words[0][0]}${words[1][0]}`.toLocaleUpperCase();
  return words[0].slice(0, 2).toLocaleUpperCase();
}

export function activePaneId(topology: SessionTopology): string | null {
  const window = topology.windows.find(item => item.isActive) ?? topology.windows[0];
  const pane = window?.panes.find(item => item.isActive) ?? window?.panes[0];
  return pane?.id ?? null;
}

export function reconcileDesktopTabs<T extends NamedDesktopTab>(
  tabs: readonly T[], sessions: readonly NamedSession[]): T[] {
  const sessionsById = new Map(sessions.map(session => [session.id, session]));
  let changed = false;
  const next = tabs.flatMap(tab => {
    const session = sessionsById.get(tab.sessionId);
    if (!session) {
      changed = true;
      return [];
    }
    if (session.name === tab.name) return [tab];
    changed = true;
    return [{ ...tab, name: session.name }];
  });
  return changed ? next : tabs as T[];
}
