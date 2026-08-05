export const TOUCH_AXIS_THRESHOLD_PIXELS = 6;
export const TOUCH_SCROLL_LINE_PIXELS = 18;
export const MAX_HISTORY_SCROLL_PAGES = 3;
export const TOUCH_LINES_PER_HISTORY_PAGE = 20;
export const APPLICATION_WHEEL_DELTA_LINES = 1;

export type TouchAxis = "pending" | "horizontal" | "vertical";
export type TerminalHistoryAction = "older" | "newer" | "latest";
export type TouchScrollRoute =
  | { kind: "history"; message: string }
  | { kind: "application"; wheelDeltaYs: number[] };

export function classifyTouchAxis(deltaX: number, deltaY: number): TouchAxis {
  if (Math.max(Math.abs(deltaX), Math.abs(deltaY)) < TOUCH_AXIS_THRESHOLD_PIXELS)
    return "pending";
  return Math.abs(deltaY) > Math.abs(deltaX) ? "vertical" : "horizontal";
}

export function serializeHistoryRequest(action: TerminalHistoryAction, pages = 1): string {
  if (action === "latest") return JSON.stringify({ type: "history", action });
  return JSON.stringify({
    type: "history",
    action,
    pages: Math.min(MAX_HISTORY_SCROLL_PAGES, Math.max(1, Math.trunc(Math.abs(pages))))
  });
}

export function historyRequestFromScrollLines(lines: number): string | null {
  if (!Number.isFinite(lines) || lines === 0) return null;
  const pages = scrollEventCount(lines);
  return serializeHistoryRequest(lines < 0 ? "older" : "newer", pages);
}

export function routeTouchScroll(lines: number, applicationScroll: boolean): TouchScrollRoute | null {
  if (!Number.isFinite(lines) || lines === 0) return null;
  if (!applicationScroll) {
    return { kind: "history", message: historyRequestFromScrollLines(lines)! };
  }

  const deltaY = lines < 0 ? -APPLICATION_WHEEL_DELTA_LINES : APPLICATION_WHEEL_DELTA_LINES;
  return { kind: "application", wheelDeltaYs: Array(scrollEventCount(lines)).fill(deltaY) };
}

function scrollEventCount(lines: number): number {
  return Math.min(MAX_HISTORY_SCROLL_PAGES,
    Math.max(1, Math.ceil(Math.abs(lines) / TOUCH_LINES_PER_HISTORY_PAGE)));
}

export function consumeTouchScroll(
  remainderPixels: number,
  fingerDeltaY: number
): { lines: number; remainderPixels: number } {
  const scrollPixels = remainderPixels - fingerDeltaY;
  const lines = Math.trunc(scrollPixels / TOUCH_SCROLL_LINE_PIXELS);
  return {
    lines,
    remainderPixels: scrollPixels - lines * TOUCH_SCROLL_LINE_PIXELS
  };
}
