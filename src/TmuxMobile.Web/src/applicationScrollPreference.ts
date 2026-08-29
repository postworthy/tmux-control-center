export const APPLICATION_SCROLL_SESSION_KEY = "tmux-mobile-application-scroll-sessions";
export const MAX_APPLICATION_SCROLL_SESSIONS = 128;

interface PreferenceStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export function parseApplicationScrollSessionIds(value: string | null): string[] {
  if (!value) return [];
  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) return [];
    return uniqueSessionIds(parsed.filter((id): id is string =>
      typeof id === "string" && id.length > 0)).slice(0, MAX_APPLICATION_SCROLL_SESSIONS);
  } catch {
    return [];
  }
}

export function readApplicationScrollSessionIds(storage: PreferenceStorage): string[] {
  try {
    return parseApplicationScrollSessionIds(storage.getItem(APPLICATION_SCROLL_SESSION_KEY));
  } catch {
    return [];
  }
}

export function isApplicationScrollEnabled(storage: PreferenceStorage, sessionId: string): boolean {
  return readApplicationScrollSessionIds(storage).includes(sessionId);
}

export function writeApplicationScrollPreference(
  storage: PreferenceStorage,
  sessionId: string,
  enabled: boolean
): void {
  if (!sessionId) return;
  const current = readApplicationScrollSessionIds(storage);
  const next = enabled
    ? [sessionId, ...current.filter((id) => id !== sessionId)].slice(0, MAX_APPLICATION_SCROLL_SESSIONS)
    : current.filter((id) => id !== sessionId);
  try {
    storage.setItem(APPLICATION_SCROLL_SESSION_KEY, JSON.stringify(next));
  } catch {
    // Storage can be unavailable or full; the current terminal still keeps its in-memory mode.
  }
}

function uniqueSessionIds(ids: readonly string[]): string[] {
  return [...new Set(ids)];
}
