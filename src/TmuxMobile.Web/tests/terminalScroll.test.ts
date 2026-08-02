import {
  classifyTouchAxis,
  consumeTouchScroll,
  historyRequestFromScrollLines,
  serializeHistoryRequest
} from "../src/terminalScroll.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(classifyTouchAxis(2, 4) === "pending", "Small movement must remain a tap candidate.");
assert(classifyTouchAxis(2, 10) === "vertical", "Vertical movement must lock to scrolling.");
assert(classifyTouchAxis(10, 2) === "horizontal", "Horizontal movement must not scroll output.");
assert(classifyTouchAxis(8, 8) === "horizontal", "Ambiguous diagonal movement must not scroll output.");

const older = consumeTouchScroll(0, 36);
assert(older.lines === -2 && older.remainderPixels === 0,
  "Dragging down must reveal two lines of older output.");

const newer = consumeTouchScroll(0, -36);
assert(newer.lines === 2 && newer.remainderPixels === 0,
  "Dragging up must move two lines toward newer output.");

const partial = consumeTouchScroll(consumeTouchScroll(0, 10).remainderPixels, 10);
assert(partial.lines === -1 && partial.remainderPixels === -2,
  "Sub-line touch movement must accumulate without losing direction.");

assert(historyRequestFromScrollLines(0) === null, "A tap must not create a history request.");
assert(historyRequestFromScrollLines(-12) === '{"type":"history","action":"older","pages":1}',
  "Downward content motion must request older tmux history.");
assert(historyRequestFromScrollLines(12) === '{"type":"history","action":"newer","pages":1}',
  "Upward content motion must request newer tmux history.");
assert(historyRequestFromScrollLines(-999) === '{"type":"history","action":"older","pages":3}',
  "History movement must be clamped before serialization.");
assert(serializeHistoryRequest("latest") === '{"type":"history","action":"latest"}',
  "Latest must be a fixed command without caller-controlled data.");

console.log("terminal touch scroll tests passed");
