export const SERVER_PROFILE_STORAGE_KEY = "tmux-mobile-server-profiles";
export const LAUNCHER_ORIGIN_SESSION_KEY = "tmux-mobile-launcher-origin";
export const MAX_SERVER_PROFILES = 32;
export const MAX_SERVER_PROFILE_DOCUMENT_BYTES = 32 * 1024;
export const MAX_SERVER_ORIGIN_LENGTH = 2048;

export interface ServerProfile {
  id: string;
  label: string;
  serverUrl: string;
}

export interface StorageReader {
  getItem(key: string): string | null;
}

export interface StorageWriter extends StorageReader {
  setItem(key: string, value: string): void;
  removeItem?(key: string): void;
}

export interface LoadedServerProfiles {
  profiles: ServerProfile[];
  error: string | null;
}

const LABEL_MAX_LENGTH = 80;
const CONTROL_CHARACTERS = /[\u0000-\u001f\u007f]/;
const PROFILE_ID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function normalizeServerOrigin(value: string): string {
  const candidate = value.trim();
  if (!candidate || candidate.length > MAX_SERVER_ORIGIN_LENGTH)
    throw new Error("Enter a server URL no longer than 2048 characters.");

  let parsed: URL;
  try {
    parsed = new URL(candidate);
  } catch {
    throw new Error("Enter a valid server URL, such as https://tmux.example.ts.net.");
  }

  if (parsed.username || parsed.password)
    throw new Error("Server URLs cannot contain a username or password.");
  if (parsed.pathname !== "/" || parsed.search || parsed.hash || candidate.includes("?") || candidate.includes("#"))
    throw new Error("Server URLs must contain only an origin, without a path, query, or fragment.");
  if (parsed.protocol !== "https:" && !(parsed.protocol === "http:" && isLoopbackHost(parsed.hostname)))
    throw new Error("Server URLs must use HTTPS. HTTP is allowed only for local development.");

  const origin = parsed.origin;
  if (origin.length > MAX_SERVER_ORIGIN_LENGTH)
    throw new Error("Enter a server URL no longer than 2048 characters.");
  return origin;
}

export function normalizeServerLabel(value: string): string {
  const label = value.trim();
  if (!label || label.length > LABEL_MAX_LENGTH || CONTROL_CHARACTERS.test(label))
    throw new Error("Labels must contain 1 to 80 visible characters.");
  return label;
}

export function loadServerProfiles(storage: StorageReader): LoadedServerProfiles {
  let raw: string | null;
  try {
    raw = storage.getItem(SERVER_PROFILE_STORAGE_KEY);
  } catch {
    return { profiles: [], error: "Saved servers are unavailable in this browser." };
  }
  if (raw === null) return { profiles: [], error: null };
  if (byteLength(raw) > MAX_SERVER_PROFILE_DOCUMENT_BYTES)
    return { profiles: [], error: "Saved server data is too large and was not loaded." };

  try {
    const document = JSON.parse(raw) as { version?: unknown; profiles?: unknown };
    if (document === null || typeof document !== "object" || document.version !== 1 || !Array.isArray(document.profiles))
      throw new Error();
    return { profiles: validateProfiles(document.profiles), error: null };
  } catch {
    return { profiles: [], error: "Saved server data is invalid and was not loaded." };
  }
}

export function saveServerProfiles(storage: StorageWriter, profiles: ServerProfile[]): ServerProfile[] {
  const validated = validateProfiles(profiles);
  const raw = JSON.stringify({ version: 1, profiles: validated });
  if (byteLength(raw) > MAX_SERVER_PROFILE_DOCUMENT_BYTES)
    throw new Error("Saved server data exceeds the 32 KiB device limit.");
  try {
    storage.setItem(SERVER_PROFILE_STORAGE_KEY, raw);
  } catch {
    throw new Error("The browser could not save these servers on this device.");
  }
  return validated;
}

export function clearServerProfiles(storage: StorageWriter): void {
  try {
    if (storage.removeItem) storage.removeItem(SERVER_PROFILE_STORAGE_KEY);
    else storage.setItem(SERVER_PROFILE_STORAGE_KEY, JSON.stringify({ version: 1, profiles: [] }));
  } catch {
    throw new Error("The browser could not clear saved servers on this device.");
  }
}

export function upsertServerProfile(
  profiles: ServerProfile[],
  input: { id?: string; label: string; serverUrl: string }
): ServerProfile[] {
  const id = input.id ?? createProfileId();
  const profile = {
    id,
    label: normalizeServerLabel(input.label),
    serverUrl: normalizeServerOrigin(input.serverUrl)
  };
  const next = input.id
    ? profiles.map((current) => current.id === input.id ? profile : current)
    : [...profiles, profile];
  if (input.id && !profiles.some((current) => current.id === input.id))
    throw new Error("That saved server no longer exists.");
  return validateProfiles(next);
}

export function deleteServerProfile(profiles: ServerProfile[], id: string): ServerProfile[] {
  return profiles.filter((profile) => profile.id !== id);
}

export function buildServerNavigationUrl(serverUrl: string, launcherOrigin: string): string {
  const target = normalizeServerOrigin(serverUrl);
  const launcher = normalizeServerOrigin(launcherOrigin);
  return `${target}/#tmuxctl-launcher=${encodeURIComponent(launcher)}`;
}

export function launcherOriginFromHash(hash: string, currentOrigin: string): string | null {
  if (!hash.startsWith("#tmuxctl-launcher=")) return null;
  const encoded = hash.slice("#tmuxctl-launcher=".length);
  if (!encoded || encoded.includes("&")) return null;
  try {
    const launcher = normalizeServerOrigin(decodeURIComponent(encoded));
    return launcher === normalizeServerOrigin(currentOrigin) ? null : launcher;
  } catch {
    return null;
  }
}

export function readLauncherOrigin(storage: StorageReader, currentOrigin: string): string | null {
  try {
    const value = storage.getItem(LAUNCHER_ORIGIN_SESSION_KEY);
    if (!value) return null;
    const normalized = normalizeServerOrigin(value);
    return normalized === normalizeServerOrigin(currentOrigin) ? null : normalized;
  } catch {
    return null;
  }
}

export function storeLauncherOrigin(storage: StorageWriter, origin: string): void {
  storage.setItem(LAUNCHER_ORIGIN_SESSION_KEY, normalizeServerOrigin(origin));
}

function validateProfiles(value: unknown[]): ServerProfile[] {
  if (value.length > MAX_SERVER_PROFILES)
    throw new Error("You can save up to 32 servers on this device.");
  const ids = new Set<string>();
  const origins = new Set<string>();
  return value.map((candidate) => {
    if (candidate === null || typeof candidate !== "object") throw new Error("Invalid saved server.");
    const raw = candidate as Partial<ServerProfile>;
    if (typeof raw.id !== "string" || !PROFILE_ID.test(raw.id)) throw new Error("Invalid saved server ID.");
    const profile = {
      id: raw.id,
      label: normalizeServerLabel(typeof raw.label === "string" ? raw.label : ""),
      serverUrl: normalizeServerOrigin(typeof raw.serverUrl === "string" ? raw.serverUrl : "")
    };
    if (ids.has(profile.id) || origins.has(profile.serverUrl)) throw new Error("Saved servers must be unique.");
    ids.add(profile.id);
    origins.add(profile.serverUrl);
    return profile;
  });
}

function createProfileId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") return crypto.randomUUID();
  throw new Error("This browser cannot create a secure saved-server identifier.");
}

function isLoopbackHost(hostname: string): boolean {
  const host = hostname.toLowerCase();
  if (host === "localhost" || host === "::1" || host === "[::1]") return true;
  const match = /^127\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/.exec(host);
  return Boolean(match && match.slice(1).every((part) => Number(part) <= 255));
}

function byteLength(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}
