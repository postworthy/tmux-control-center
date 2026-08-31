export const SETTLED_TERMINAL_REFIT_DELAYS = [50, 200] as const;
export const TERMINAL_GEOMETRY_POLL_MILLISECONDS = 100;
export const NATIVE_WINDOW_GEOMETRY_EVENT = "tmuxctl:native-window-geometry-changed";
export const MINIMUM_TERMINAL_COLUMNS = 10;
export const MAXIMUM_TERMINAL_COLUMNS = 2048;
export const MINIMUM_TERMINAL_ROWS = 5;
export const MAXIMUM_TERMINAL_ROWS = 1024;

export function boundedTerminalGrid(columns: number, rows: number): { columns: number; rows: number } {
  return {
    columns: Math.min(MAXIMUM_TERMINAL_COLUMNS,
      Math.max(MINIMUM_TERMINAL_COLUMNS, Math.trunc(columns))),
    rows: Math.min(MAXIMUM_TERMINAL_ROWS,
      Math.max(MINIMUM_TERMINAL_ROWS, Math.trunc(rows)))
  };
}

export function terminalHostCanBeFit(width: number, height: number): boolean {
  return Number.isFinite(width) && Number.isFinite(height) && width > 0 && height > 0;
}

export function terminalHostGeometryKey(width: number, height: number): string | null {
  return terminalHostCanBeFit(width, height) ? `${width}x${height}` : null;
}
