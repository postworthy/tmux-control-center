import type { InventorySnapshot, TmuxSession } from "../src/types.js";

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

export interface TmuxPane {
  id: string;
  windowId: string;
  paneIndex: number;
  title: string;
  currentCommand: string;
  currentWorkingDirectory: string;
  isActive: boolean;
  width: number;
  height: number;
}

export interface TmuxWindow {
  id: string;
  sessionId: string;
  index: number;
  name: string;
  isActive: boolean;
  layout: string;
  panes: TmuxPane[];
}

export interface TmuxTopology { sessionId: string; windows: TmuxWindow[]; }

export const getTopology = (sessionId: string) =>
  request<TmuxTopology>(`/api/sessions/${encodeURIComponent(sessionId)}/topology`);

async function topologyMutation<T>(path: string, method: "POST" | "DELETE", body?: unknown): Promise<T> {
  return request<T>(path, {
    method,
    body: body === undefined ? undefined : JSON.stringify(body),
    headers: { "X-CSRF-TOKEN": await csrf() }
  });
}

export const createWindow = (sessionId: string, name?: string) =>
  topologyMutation<{ id: string }>(`/api/sessions/${encodeURIComponent(sessionId)}/windows`, "POST", { name });
export const selectWindow = (windowId: string) =>
  topologyMutation<void>(`/api/windows/${encodeURIComponent(windowId)}/select`, "POST", {});
export const killWindow = (windowId: string) =>
  topologyMutation<void>(`/api/windows/${encodeURIComponent(windowId)}`, "DELETE");
export const splitPane = (paneId: string, orientation: "horizontal" | "vertical") =>
  topologyMutation<{ id: string }>(`/api/panes/${encodeURIComponent(paneId)}/split`, "POST", { orientation });
export const selectPane = (paneId: string) =>
  topologyMutation<void>(`/api/panes/${encodeURIComponent(paneId)}/select`, "POST", {});
export const resizePane = (paneId: string, direction: "left" | "right" | "up" | "down", cells = 2) =>
  topologyMutation<void>(`/api/panes/${encodeURIComponent(paneId)}/resize`, "POST", { direction, cells });
export const killPane = (paneId: string) =>
  topologyMutation<void>(`/api/panes/${encodeURIComponent(paneId)}`, "DELETE");

export async function createSession(name: string): Promise<{ id: string; name: string }> {
  return request("/api/sessions", {
    method: "POST",
    body: JSON.stringify({ name }),
    headers: { "X-CSRF-TOKEN": await csrf() }
  });
}

export async function renameSession(sessionId: string, name: string): Promise<void> {
  await request(`/api/sessions/${encodeURIComponent(sessionId)}/rename`, {
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
