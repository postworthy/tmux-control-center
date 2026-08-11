interface NamedSession { name: string; }

export function filterSessionsByName<T extends NamedSession>(sessions: readonly T[], query: string): T[] {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return [...sessions];
  return sessions.filter((session) => session.name.toLowerCase().includes(normalized));
}
