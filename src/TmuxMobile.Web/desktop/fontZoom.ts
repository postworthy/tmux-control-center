export const DEFAULT_TERMINAL_FONT_SIZE = 14;
export const MIN_TERMINAL_FONT_SIZE = 8;
export const MAX_TERMINAL_FONT_SIZE = 32;

export function nextTerminalFontSize(current: number, wheelDeltaY: number): number {
  const normalized = Number.isFinite(current) ? Math.round(current) : DEFAULT_TERMINAL_FONT_SIZE;
  const direction = wheelDeltaY < 0 ? 1 : wheelDeltaY > 0 ? -1 : 0;
  return Math.min(MAX_TERMINAL_FONT_SIZE, Math.max(MIN_TERMINAL_FONT_SIZE, normalized + direction));
}

export function terminalFontSizeForWheel(
  current: number, wheelDeltaY: number, controlPressed: boolean
): number | null {
  return controlPressed ? nextTerminalFontSize(current, wheelDeltaY) : null;
}
