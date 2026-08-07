export const SESSION_RECENCY_KEY = "tmux-mobile-session-recency";

interface SessionIdentity { id: string; }
interface RecencyStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export function parseSessionRecency(value: string | null): string[] {
  if (!value) return [];
  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) return [];
    return uniqueIds(parsed.filter((id): id is string => typeof id === "string" && id.length > 0));
  } catch {
    return [];
  }
}

export function readSessionRecency(storage: RecencyStorage): string[] {
  try {
    return parseSessionRecency(storage.getItem(SESSION_RECENCY_KEY));
  } catch {
    return [];
  }
}

export function writeSessionRecency(storage: RecencyStorage, ids: readonly string[]): void {
  try {
    storage.setItem(SESSION_RECENCY_KEY, JSON.stringify(uniqueIds(ids)));
  } catch {
    // Storage can be unavailable or full; in-memory ordering still works.
  }
}

export function promoteSessionRecency(ids: readonly string[], sessionId: string): string[] {
  return [sessionId, ...ids.filter((id) => id !== sessionId)];
}

export function pruneSessionRecency(sessions: readonly SessionIdentity[], ids: readonly string[]): string[] {
  const liveIds = new Set(sessions.map((session) => session.id));
  return uniqueIds(ids).filter((id) => liveIds.has(id));
}

export function orderSessionsByRecency<T extends SessionIdentity>(
  sessions: readonly T[],
  ids: readonly string[]
): T[] {
  const byId = new Map(sessions.map((session) => [session.id, session]));
  const ordered = uniqueIds(ids).flatMap((id) => {
    const session = byId.get(id);
    return session ? [session] : [];
  });
  const rankedIds = new Set(ordered.map((session) => session.id));
  return [...ordered, ...sessions.filter((session) => !rankedIds.has(session.id))];
}

function uniqueIds(ids: readonly string[]): string[] {
  return [...new Set(ids)];
}
