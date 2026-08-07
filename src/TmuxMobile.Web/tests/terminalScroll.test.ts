import {
  applicationWheelMultiplier,
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

assert(applicationWheelMultiplier(0.49) === 1 && applicationWheelMultiplier(0.5) === 2,
  "Medium swipe velocity must start at 0.5 pixels per millisecond.");
assert(applicationWheelMultiplier(0.99) === 2 && applicationWheelMultiplier(1) === 3,
  "Fast swipe velocity must start at 1 pixel per millisecond.");
assert(applicationWheelMultiplier(1.49) === 3 && applicationWheelMultiplier(1.5) === 4,
  "Very fast swipe velocity must start at 1.5 pixels per millisecond.");
assert(applicationWheelMultiplier(Number.NaN) === 1 && applicationWheelMultiplier(-1) === 1,
  "Invalid velocity must fall back to the precise 1x multiplier.");

const fastApplicationSwipe = routeTouchScroll(-5, true, 1.5);
assert(fastApplicationSwipe?.kind === "application" &&
  fastApplicationSwipe.wheelDeltaYs.length === 20 &&
  fastApplicationSwipe.wheelDeltaYs.every((deltaY) => deltaY === -1),
  "A very fast swipe must move four times farther than the same deliberate drag.");

const applicationCapped = routeTouchScroll(-999, true);
assert(applicationCapped?.kind === "application" && applicationCapped.wheelDeltaYs.length === 72,
  "Application swipes must retain a bounded 72-event maximum.");

const fastApplicationCapped = routeTouchScroll(30, true, 2);
assert(fastApplicationCapped?.kind === "application" &&
  fastApplicationCapped.wheelDeltaYs.length === 72,
  "The velocity multiplier must not bypass the application event cap.");

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
