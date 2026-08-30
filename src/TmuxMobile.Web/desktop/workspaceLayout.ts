export type WorkspaceDropZone = "center" | "left" | "right" | "top" | "bottom";

export interface WorkspaceGroup {
  kind: "group";
  id: string;
  tabIds: string[];
  activeId: string | null;
}

export interface WorkspaceSplit {
  kind: "split";
  id: string;
  direction: "row" | "column";
  first: WorkspaceNode;
  second: WorkspaceNode;
}

export type WorkspaceNode = WorkspaceGroup | WorkspaceSplit;

export const createWorkspace = (groupId: string): WorkspaceGroup =>
  ({ kind: "group", id: groupId, tabIds: [], activeId: null });

export function workspaceGroups(node: WorkspaceNode): WorkspaceGroup[] {
  return node.kind === "group"
    ? [node]
    : [...workspaceGroups(node.first), ...workspaceGroups(node.second)];
}

export function workspaceGroup(node: WorkspaceNode, groupId: string): WorkspaceGroup | null {
  return workspaceGroups(node).find(group => group.id === groupId) ?? null;
}

export function groupForSession(node: WorkspaceNode, sessionId: string): WorkspaceGroup | null {
  return workspaceGroups(node).find(group => group.tabIds.includes(sessionId)) ?? null;
}

function updateGroup(node: WorkspaceNode, groupId: string,
  update: (group: WorkspaceGroup) => WorkspaceNode): WorkspaceNode {
  if (node.kind === "group") return node.id === groupId ? update(node) : node;
  return {
    ...node,
    first: updateGroup(node.first, groupId, update),
    second: updateGroup(node.second, groupId, update)
  };
}

function removeSessionFromGroups(node: WorkspaceNode, sessionId: string): WorkspaceNode {
  if (node.kind === "split") return {
    ...node,
    first: removeSessionFromGroups(node.first, sessionId),
    second: removeSessionFromGroups(node.second, sessionId)
  };
  const index = node.tabIds.indexOf(sessionId);
  if (index < 0) return node;
  const tabIds = node.tabIds.filter(id => id !== sessionId);
  const activeId = node.activeId === sessionId
    ? tabIds[Math.min(index, tabIds.length - 1)] ?? null
    : node.activeId;
  return { ...node, tabIds, activeId };
}

function collapseEmpty(node: WorkspaceNode, isRoot = true): WorkspaceNode | null {
  if (node.kind === "group") return node.tabIds.length || isRoot ? node : null;
  const first = collapseEmpty(node.first, false);
  const second = collapseEmpty(node.second, false);
  if (!first) return second;
  if (!second) return first;
  return { ...node, first, second };
}

export function openWorkspaceSession(node: WorkspaceNode, preferredGroupId: string,
  sessionId: string): WorkspaceNode {
  const existing = groupForSession(node, sessionId);
  const target = existing ?? workspaceGroup(node, preferredGroupId) ?? workspaceGroups(node)[0];
  if (!target) return node;
  return updateGroup(node, target.id, group => ({
    ...group,
    tabIds: group.tabIds.includes(sessionId) ? group.tabIds : [...group.tabIds, sessionId],
    activeId: sessionId
  }));
}

export function activateWorkspaceSession(node: WorkspaceNode, groupId: string,
  sessionId: string): WorkspaceNode {
  return updateGroup(node, groupId, group => group.tabIds.includes(sessionId)
    ? { ...group, activeId: sessionId }
    : group);
}

export function closeWorkspaceSession(node: WorkspaceNode, sessionId: string): WorkspaceNode {
  return collapseEmpty(removeSessionFromGroups(node, sessionId)) ?? createWorkspace("group-root");
}

export function pruneWorkspaceSessions(node: WorkspaceNode, liveSessionIds: ReadonlySet<string>): WorkspaceNode {
  const prune = (current: WorkspaceNode): WorkspaceNode => current.kind === "split"
    ? { ...current, first: prune(current.first), second: prune(current.second) }
    : {
        ...current,
        tabIds: current.tabIds.filter(id => liveSessionIds.has(id)),
        activeId: current.activeId && liveSessionIds.has(current.activeId)
          ? current.activeId
          : current.tabIds.find(id => liveSessionIds.has(id)) ?? null
      };
  return collapseEmpty(prune(node)) ?? createWorkspace("group-root");
}

export function moveWorkspaceSession(node: WorkspaceNode, sessionId: string,
  targetGroupId: string, zone: WorkspaceDropZone, newGroupId: string,
  newSplitId: string): WorkspaceNode {
  const source = groupForSession(node, sessionId);
  const target = workspaceGroup(node, targetGroupId);
  if (!source || !target) return node;
  if (zone !== "center" && source.id === target.id && source.tabIds.length === 1) return node;

  const withoutSession = removeSessionFromGroups(node, sessionId);
  if (zone === "center") {
    const moved = updateGroup(withoutSession, targetGroupId, group => ({
      ...group,
      tabIds: [...group.tabIds, sessionId],
      activeId: sessionId
    }));
    return collapseEmpty(moved) ?? createWorkspace("group-root");
  }

  const newGroup: WorkspaceGroup = { kind: "group", id: newGroupId, tabIds: [sessionId], activeId: sessionId };
  const split = updateGroup(withoutSession, targetGroupId, group => {
    const before = zone === "left" || zone === "top";
    return {
      kind: "split",
      id: newSplitId,
      direction: zone === "left" || zone === "right" ? "row" : "column",
      first: before ? newGroup : group,
      second: before ? group : newGroup
    };
  });
  return collapseEmpty(split) ?? createWorkspace("group-root");
}

export function dropZoneForPoint(width: number, height: number,
  x: number, y: number): WorkspaceDropZone {
  if (width <= 0 || height <= 0) return "center";
  const candidates = [
    { zone: "left" as const, distance: x / width },
    { zone: "right" as const, distance: (width - x) / width },
    { zone: "top" as const, distance: y / height },
    { zone: "bottom" as const, distance: (height - y) / height }
  ].sort((a, b) => a.distance - b.distance);
  return candidates[0].distance <= .25 ? candidates[0].zone : "center";
}
