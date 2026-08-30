export const SETTLED_TERMINAL_REFIT_DELAYS = [50, 200] as const;
export const TERMINAL_GEOMETRY_POLL_MILLISECONDS = 100;

export function terminalHostCanBeFit(width: number, height: number): boolean {
  return Number.isFinite(width) && Number.isFinite(height) && width > 0 && height > 0;
}

export function terminalHostGeometryKey(width: number, height: number): string | null {
  return terminalHostCanBeFit(width, height) ? `${width}x${height}` : null;
}
