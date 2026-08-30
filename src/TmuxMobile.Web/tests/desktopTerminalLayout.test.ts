import {
  SETTLED_TERMINAL_REFIT_DELAYS,
  terminalHostCanBeFit
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

console.log("desktop terminal layout tests passed");
