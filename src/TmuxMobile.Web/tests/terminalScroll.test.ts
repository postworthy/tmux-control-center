import {
  classifyTouchAxis,
  consumeTouchScroll,
  historyRequestFromScrollLines,
  routeScrollControl,
  routeTouchScroll,
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

const defaultRoute = routeTouchScroll(-999, false);
assert(defaultRoute?.kind === "history" &&
  defaultRoute.message === '{"type":"history","action":"older","pages":3}',
"Default swipe routing must retain the existing bounded tmux history message.");

const applicationOlder = routeTouchScroll(-5, true);
assert(applicationOlder?.kind === "application" &&
  applicationOlder.wheelDeltaYs.length === 5 &&
  applicationOlder.wheelDeltaYs.every((deltaY) => deltaY === -1),
"Enabled downward swipes must produce one wheel-up event per consumed touch line.");

const applicationNewer = routeTouchScroll(12, true);
assert(applicationNewer?.kind === "application" &&
  applicationNewer.wheelDeltaYs.length === 12 && applicationNewer.wheelDeltaYs[0] === 1,
"Enabled upward swipes must scale wheel-down events with swipe distance.");

const applicationCapped = routeTouchScroll(-999, true);
assert(applicationCapped?.kind === "application" && applicationCapped.wheelDeltaYs.length === 24,
  "Application swipes must retain a bounded 24-event maximum.");

const defaultOlderControl = routeScrollControl("older", false);
assert(defaultOlderControl.kind === "history" &&
  defaultOlderControl.message === '{"type":"history","action":"older","pages":1}',
  "Older must retain its exact tmux-history command while application scrolling is off.");

const defaultLatestControl = routeScrollControl("latest", false);
assert(defaultLatestControl.kind === "history" &&
  defaultLatestControl.message === '{"type":"history","action":"latest"}',
  "Latest must retain its exact tmux-history command while application scrolling is off.");

const applicationOlderControl = routeScrollControl("older", true);
assert(applicationOlderControl.kind === "application" &&
  applicationOlderControl.wheelDeltaYs.length === 12 &&
  applicationOlderControl.wheelDeltaYs.every((deltaY) => deltaY === -1),
  "Older must become a fixed wheel-up burst while application scrolling is on.");

const applicationLatestControl = routeScrollControl("latest", true);
assert(applicationLatestControl.kind === "application" &&
  applicationLatestControl.wheelDeltaYs.length === 12 &&
  applicationLatestControl.wheelDeltaYs.every((deltaY) => deltaY === 1),
  "Latest must become a fixed wheel-down burst while application scrolling is on.");

assert(routeTouchScroll(0, true) === null && routeTouchScroll(Number.NaN, true) === null,
  "Tap and invalid movement must never produce application input.");

console.log("terminal touch scroll tests passed");
