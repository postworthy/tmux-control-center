import type { InventorySnapshot, TmuxSession } from "../src/types";

let csrfToken: string | null = null;

export class UnauthorizedError extends Error {}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: "same-origin",
    cache: "no-store",
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers }
  });
  if (response.status === 401) throw new UnauthorizedError("Authentication required");
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { error?: string; detail?: string } | null;
    throw new Error(detail?.error ?? detail?.detail ?? `Request failed (${response.status})`);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

async function csrf(): Promise<string> {
  if (csrfToken) return csrfToken;
  csrfToken = (await request<{ token: string }>("/api/auth/csrf")).token;
  return csrfToken;
}

export async function login(apiKey: string): Promise<void> {
  await request("/api/auth/login", { method: "POST", body: JSON.stringify({ apiKey }) });
  csrfToken = null;
}

export const getSessions = () => request<TmuxSession[]>("/api/sessions");

export async function createSession(name: string): Promise<{ id: string; name: string }> {
  return request("/api/sessions", {
    method: "POST",
    body: JSON.stringify({ name }),
    headers: { "X-CSRF-TOKEN": await csrf() }
  });
}

export async function killSession(sessionId: string): Promise<void> {
  await request(`/api/sessions/${encodeURIComponent(sessionId)}`, {
    method: "DELETE",
    headers: { "X-CSRF-TOKEN": await csrf() }
  });
}

export function terminalWebSocketUrl(sessionId: string): string {
  const protocol = location.protocol === "https:" ? "wss:" : "ws:";
  return `${protocol}//${location.host}/ws/terminal/${encodeURIComponent(sessionId)}`;
}

export function inventoryWebSocketUrl(): string {
  const protocol = location.protocol === "https:" ? "wss:" : "ws:";
  return `${protocol}//${location.host}/ws/inventory`;
}

export type { InventorySnapshot, TmuxSession };
