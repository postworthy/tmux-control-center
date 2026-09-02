import { activePaneId, reconcileDesktopTabs, sessionIconLabel } from "../desktop/desktopNavigation.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(sessionIconLabel("project-alpha") === "PA", "Separated session names use initials.");
assert(sessionIconLabel("shell") === "SH", "Single-word session names remain identifiable.");
assert(sessionIconLabel("  ") === "?", "Blank names have a safe icon fallback.");

const pane = (id: string, isActive: boolean) => ({
  id, windowId: "@1", paneIndex: 0, title: "", currentCommand: "bash",
  currentWorkingDirectory: "/tmp", isActive, width: 80, height: 24
});
const topology = {
  sessionId: "session",
  windows: [
    { id: "@0", sessionId: "session", index: 0, name: "one", isActive: false, layout: "", panes: [pane("%0", true)] },
    { id: "@1", sessionId: "session", index: 1, name: "two", isActive: true, layout: "", panes: [pane("%1", false), pane("%2", true)] }
  ]
};
assert(activePaneId(topology) === "%2", "Split targets the active pane in the active window.");
assert(activePaneId({ sessionId: "empty", windows: [] }) === null,
  "Missing topology does not invent a pane target.");

const tabs = [{ sessionId: "alpha", name: "alpha" }, { sessionId: "stale", name: "stale" }];
const reconciled = reconcileDesktopTabs(tabs, [{ id: "alpha", name: "renamed-alpha" }]);
assert(reconciled.length === 1 && reconciled[0].name === "renamed-alpha",
  "Inventory rename updates open tab labels while stale sessions are removed.");
assert(reconcileDesktopTabs(reconciled, [{ id: "alpha", name: "renamed-alpha" }]) === reconciled,
  "Unchanged inventory preserves the existing tab collection.");

console.log("desktop navigation tests passed");
