import {
  DESKTOP_HISTORY_FLUSH_MILLISECONDS,
  historyRequestFromWheelDelta
} from "../desktop/terminalWheel.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(historyRequestFromWheelDelta(-1) === '{"type":"history","action":"older","pages":1}',
  "Wheel up must request one older tmux history page.");
assert(historyRequestFromWheelDelta(1) === '{"type":"history","action":"newer","pages":1}',
  "Wheel down must request one newer tmux history page.");
assert(historyRequestFromWheelDelta(-250) === '{"type":"history","action":"older","pages":3}',
  "Accumulated wheel-up input must remain bounded to three pages.");
assert(historyRequestFromWheelDelta(500) === '{"type":"history","action":"newer","pages":3}',
  "Accumulated wheel-down input must remain bounded to three pages.");
assert(historyRequestFromWheelDelta(0) === null && historyRequestFromWheelDelta(Number.NaN) === null,
  "Zero and invalid wheel deltas must not emit history operations.");
assert(DESKTOP_HISTORY_FLUSH_MILLISECONDS >= 250,
  "Wheel history dispatch must not exceed the server's four-operation-per-second limit.");

console.log("desktop terminal wheel tests passed");
