import { serializeHistoryRequest } from "../src/terminalScroll.js";

export const DESKTOP_HISTORY_FLUSH_MILLISECONDS = 250;
export const DESKTOP_APPLICATION_WHEEL_FLUSH_MILLISECONDS = 25;
const WHEEL_DELTA_PER_PAGE = 100;
const MAX_HISTORY_PAGES = 3;

export type DesktopWheelRoute = "zoom" | "application" | "history" | "ignore";

export function routeDesktopWheel(
  deltaY: number, controlPressed: boolean, applicationScrollEnabled: boolean
): DesktopWheelRoute {
  if (controlPressed) return "zoom";
  if (!Number.isFinite(deltaY) || deltaY === 0) return "ignore";
  return applicationScrollEnabled ? "application" : "history";
}

export function historyRequestFromWheelDelta(deltaY: number): string | null {
  if (!Number.isFinite(deltaY) || deltaY === 0) return null;
  const pages = Math.min(MAX_HISTORY_PAGES,
    Math.max(1, Math.ceil(Math.abs(deltaY) / WHEEL_DELTA_PER_PAGE)));
  return serializeHistoryRequest(deltaY < 0 ? "older" : "newer", pages);
}
