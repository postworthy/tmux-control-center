import {
  activateWorkspaceSession,
  closeWorkspaceSession,
  createWorkspace,
  dropZoneForPoint,
  groupForSession,
  openWorkspaceSession,
  pruneWorkspaceSessions,
  resetWorkspaceLayout,
  splitWorkspaceSessionAtRoot,
  WORKSPACE_DROP_ZONES,
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

layout = splitWorkspaceSessionAtRoot(layout, "beta", "right", "g1", "s1");
assert(layout.kind === "split" && layout.direction === "row" && workspaceGroups(layout).length === 2,
  "A global right-edge drop creates a side-by-side root split.");
assert(groupForSession(layout, "alpha")?.id === "g0" && groupForSession(layout, "beta")?.id === "g1",
  "A split moves the dragged session without duplicating it.");

layout = openWorkspaceSession(layout, "g1", "gamma");
layout = splitWorkspaceSessionAtRoot(layout, "gamma", "bottom", "g2", "s2");
assert(workspaceGroups(layout).length === 3 && groupForSession(layout, "gamma")?.id === "g2",
  "Another global edge drop keeps the prior tree and creates a nested layout.");
assert(layout.kind === "split" && layout.direction === "column" && layout.second.kind === "group" &&
  layout.second.id === "g2", "A bottom drop places the new group below the complete prior layout.");

layout = resetWorkspaceLayout(layout, "standard", "gamma");
assert(layout.kind === "group" && layout.tabIds.join(",") === "alpha,beta,gamma",
  "Single view flattens every open tab into stable visual order.");
assert(layout.activeId === "gamma", "Single view preserves the preferred active session.");
const resetAgain = resetWorkspaceLayout(layout, "standard", "gamma");
assert(JSON.stringify(resetAgain) === JSON.stringify(layout), "Single view is idempotent.");

layout = splitWorkspaceSessionAtRoot(layout, "beta", "left", "g3", "s3");
assert(groupForSession(layout, "beta")?.id === "g3" && workspaceGroups(layout).length === 2,
  "A later edge split remains relative to the entire standard layout.");
assert(workspaceGroups(layout).flatMap(group => group.tabIds).filter(id => id === "beta").length === 1,
  "A session appears in exactly one editor group.");

layout = activateWorkspaceSession(layout, "g3", "beta");
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
assert(WORKSPACE_DROP_ZONES.length === 5 && new Set(WORKSPACE_DROP_ZONES).size === 5,
  "The workspace exposes exactly one canonical set of five unique snap zones.");

const single = openWorkspaceSession(createWorkspace("only"), "only", "alpha");
assert(splitWorkspaceSessionAtRoot(single, "alpha", "right", "unused", "unused") === single,
  "A lone tab cannot create an empty companion split.");

console.log("desktop workspace layout tests passed");
