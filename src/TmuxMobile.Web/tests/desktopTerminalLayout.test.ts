import {
  MAXIMUM_TERMINAL_COLUMNS,
  MAXIMUM_TERMINAL_ROWS,
  MINIMUM_TERMINAL_COLUMNS,
  MINIMUM_TERMINAL_ROWS,
  NATIVE_WINDOW_GEOMETRY_EVENT,
  SETTLED_TERMINAL_REFIT_DELAYS,
  TERMINAL_GEOMETRY_POLL_MILLISECONDS,
  boundedTerminalGrid,
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
assert(MAXIMUM_TERMINAL_COLUMNS === 2048 && MAXIMUM_TERMINAL_ROWS === 1024 &&
  MINIMUM_TERMINAL_COLUMNS === 10 && MINIMUM_TERMINAL_ROWS === 5,
  "Desktop terminal bounds must match the server PTY contract.");
const fiveKilopixelGrid = boundedTerminalGrid(1067, 480);
assert(fiveKilopixelGrid.columns === 1067 && fiveKilopixelGrid.rows === 480,
  "A 5K display at minimum font size must retain its complete fitted grid.");
const boundedGrid = boundedTerminalGrid(4096, 2048);
assert(boundedGrid.columns === MAXIMUM_TERMINAL_COLUMNS && boundedGrid.rows === MAXIMUM_TERMINAL_ROWS,
  "Oversized terminal grids must remain within the finite server contract.");

console.log("desktop terminal layout tests passed");
