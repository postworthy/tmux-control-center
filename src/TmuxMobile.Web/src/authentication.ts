// A 401 remains authoritative until an explicit successful sign-in.
// Inventory WebSocket messages cannot restore HTTP authentication.
let required = false;
const listeners = new Set<() => void>();

export const authenticationRequired = () => required;
export function subscribeAuthentication(listener: () => void): () => void {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}
export function setAuthenticationRequired(value: boolean): void {
  if (required === value) return;
  required = value;
  for (const listener of listeners) listener();
}
