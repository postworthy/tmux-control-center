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
