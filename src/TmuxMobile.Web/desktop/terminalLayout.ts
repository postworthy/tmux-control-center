export const SETTLED_TERMINAL_REFIT_DELAYS = [50, 200] as const;

export function terminalHostCanBeFit(width: number, height: number): boolean {
  return Number.isFinite(width) && Number.isFinite(height) && width > 0 && height > 0;
}
