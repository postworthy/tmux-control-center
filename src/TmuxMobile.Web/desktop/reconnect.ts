export const RECONNECT_STABILITY_MILLISECONDS = 10_000;

export function reconnectDelay(attempt: number): number {
  const boundedAttempt = Math.max(0, Math.min(5, Math.trunc(attempt)));
  return Math.min(30_000, 1_000 * 2 ** boundedAttempt);
}

export function nextReconnectAttempt(attempt: number): number {
  return Math.min(5, Math.max(0, Math.trunc(attempt)) + 1);
}

export function isTerminalPing(value: unknown): boolean {
  if (typeof value !== "string") return false;
  try {
    const message = JSON.parse(value) as { type?: unknown };
    return message.type === "ping";
  } catch {
    return false;
  }
}
