import {
  NATIVE_WINDOW_GEOMETRY_EVENT,
  SETTLED_TERMINAL_REFIT_DELAYS,
  TERMINAL_GEOMETRY_POLL_MILLISECONDS,
  terminalHostCanBeFit,
  terminalHostGeometryKey
} from "../desktop/terminalLayout.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(!terminalHostCanBeFit(0, 500) && !terminalHostCanBeFit(800, 0),
  "A hidden or unmeasured terminal must not be fit.");
assert(terminalHostCanBeFit(800, 500),
  "A laid-out terminal host must be eligible for fitting.");
assert(SETTLED_TERMINAL_REFIT_DELAYS.length >= 2 &&
  SETTLED_TERMINAL_REFIT_DELAYS.every(delay => delay > 0),
  "Initial activation must include delayed layout-settling refits.");
assert(terminalHostGeometryKey(800, 500) === terminalHostGeometryKey(800, 500),
  "Stable native geometry must not look changed.");
assert(terminalHostGeometryKey(801, 500) !== terminalHostGeometryKey(800, 500) &&
  terminalHostGeometryKey(800, 501) !== terminalHostGeometryKey(800, 500),
  "Either native host dimension must produce a new geometry key.");
assert(terminalHostGeometryKey(0, 500) === null && TERMINAL_GEOMETRY_POLL_MILLISECONDS >= 50,
  "Hidden terminals must be ignored and geometry polling must remain bounded.");
assert(NATIVE_WINDOW_GEOMETRY_EVENT === "tmuxctl:native-window-geometry-changed",
  "Native window geometry must use a stable app-wide refit event.");

console.log("desktop terminal layout tests passed");
