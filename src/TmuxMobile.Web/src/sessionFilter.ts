interface FilterableSession { name: string; isAttached: boolean; }

export function filterSessionsByName<T extends Pick<FilterableSession, "name">>(
  sessions: readonly T[], query: string
): T[] {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return [...sessions];
  return sessions.filter((session) => session.name.toLowerCase().includes(normalized));
}

export function filterSessions<T extends FilterableSession>(
  sessions: readonly T[], query: string, detachedOnly: boolean
): T[] {
  const byAttachment = detachedOnly
    ? sessions.filter((session) => !session.isAttached)
    : sessions;
  return filterSessionsByName(byAttachment, query);
}
