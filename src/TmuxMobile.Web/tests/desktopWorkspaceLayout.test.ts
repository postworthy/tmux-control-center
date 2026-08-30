import {
  activateWorkspaceSession,
  closeWorkspaceSession,
  createWorkspace,
  dropZoneForPoint,
  groupForSession,
  moveWorkspaceSession,
  openWorkspaceSession,
  pruneWorkspaceSessions,
  workspaceGroups,
  type WorkspaceNode
} from "../desktop/workspaceLayout.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

let layout: WorkspaceNode = createWorkspace("g0");
layout = openWorkspaceSession(layout, "g0", "alpha");
layout = openWorkspaceSession(layout, "g0", "beta");
assert(workspaceGroups(layout).length === 1 && groupForSession(layout, "beta")?.activeId === "beta",
  "Opening sessions adds unique tabs to the focused group.");

layout = moveWorkspaceSession(layout, "beta", "g0", "right", "g1", "s1");
assert(layout.kind === "split" && layout.direction === "row" && workspaceGroups(layout).length === 2,
  "A right-edge drop creates a side-by-side editor split.");
assert(groupForSession(layout, "alpha")?.id === "g0" && groupForSession(layout, "beta")?.id === "g1",
  "A split moves the dragged session without duplicating it.");

layout = openWorkspaceSession(layout, "g1", "gamma");
layout = moveWorkspaceSession(layout, "gamma", "g1", "bottom", "g2", "s2");
assert(workspaceGroups(layout).length === 3 && groupForSession(layout, "gamma")?.id === "g2",
  "Nested top/bottom groups support custom layouts.");

layout = moveWorkspaceSession(layout, "beta", "g2", "center", "unused", "unused");
assert(workspaceGroups(layout).length === 2 && groupForSession(layout, "beta")?.id === "g2",
  "Moving the last tab out of a group collapses the empty group.");
assert(workspaceGroups(layout).flatMap(group => group.tabIds).filter(id => id === "beta").length === 1,
  "A session appears in exactly one editor group.");

layout = activateWorkspaceSession(layout, "g2", "beta");
assert(groupForSession(layout, "beta")?.activeId === "beta", "A group tracks its visible session.");
layout = closeWorkspaceSession(layout, "beta");
assert(groupForSession(layout, "beta") === null, "Closing a tab removes only that session.");
layout = pruneWorkspaceSessions(layout, new Set(["alpha"]));
assert(workspaceGroups(layout).length === 1 && groupForSession(layout, "alpha") !== null,
  "Inventory pruning collapses stale empty groups.");

assert(dropZoneForPoint(1000, 800, 10, 400) === "left" &&
  dropZoneForPoint(1000, 800, 990, 400) === "right" &&
  dropZoneForPoint(1000, 800, 500, 10) === "top" &&
  dropZoneForPoint(1000, 800, 500, 790) === "bottom" &&
  dropZoneForPoint(1000, 800, 500, 400) === "center",
  "Pointer geometry selects the four snap edges and center move target.");

console.log("desktop workspace layout tests passed");
