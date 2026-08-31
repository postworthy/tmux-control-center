import {
  buildServerNavigationUrl,
  clearServerProfiles,
  deleteServerProfile,
  launcherOriginFromHash,
  loadServerProfiles,
  MAX_SERVER_PROFILE_DOCUMENT_BYTES,
  normalizeServerOrigin,
  saveServerProfiles,
  SERVER_PROFILE_STORAGE_KEY,
  upsertServerProfile
} from "../src/serverProfiles.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

function expectError(action: () => unknown, part: string) {
  try {
    action();
  } catch (reason) {
    assert(reason instanceof Error && reason.message.includes(part), `Expected an error containing “${part}”.`);
    return;
  }
  throw new Error(`Expected an error containing “${part}”.`);
}

assert(normalizeServerOrigin(" https://Tmux.Example.com:443/ ") === "https://tmux.example.com",
  "HTTPS origins must be normalized without a default port or trailing slash.");
assert(normalizeServerOrigin("http://127.8.9.10:5173") === "http://127.8.9.10:5173",
  "HTTP must remain available for loopback development.");
assert(normalizeServerOrigin("http://[::1]:5173") === "http://[::1]:5173",
  "IPv6 loopback must remain available for development.");
expectError(() => normalizeServerOrigin("http://example.com"), "HTTPS");
expectError(() => normalizeServerOrigin("https://user:secret@example.com"), "username or password");
expectError(() => normalizeServerOrigin("https://example.com/api"), "only an origin");
expectError(() => normalizeServerOrigin("https://example.com?"), "only an origin");
expectError(() => normalizeServerOrigin("https://example.com/#return"), "only an origin");

let storedValue: string | null = null;
const storage = {
  getItem: (key: string) => key === SERVER_PROFILE_STORAGE_KEY ? storedValue : null,
  setItem: (key: string, value: string) => { if (key === SERVER_PROFILE_STORAGE_KEY) storedValue = value; },
  removeItem: (key: string) => { if (key === SERVER_PROFILE_STORAGE_KEY) storedValue = null; }
};

const first = upsertServerProfile([], {
  label: " Ubuntu box ",
  serverUrl: "https://ubuntu.example.ts.net:8443/"
});
assert(first[0].label === "Ubuntu box" && first[0].serverUrl === "https://ubuntu.example.ts.net:8443",
  "Profiles must normalize their label and origin.");
saveServerProfiles(storage, first);
assert(loadServerProfiles(storage).profiles[0].label === "Ubuntu box",
  "Saved profiles must survive a storage round trip.");

const edited = upsertServerProfile(first, {
  id: first[0].id,
  label: "Primary",
  serverUrl: first[0].serverUrl
});
assert(edited[0].label === "Primary", "Editing must retain the profile identity and update its label.");
assert(deleteServerProfile(edited, edited[0].id).length === 0, "Deleting must remove only the selected profile.");
expectError(() => upsertServerProfile(first, {
  label: "Duplicate",
  serverUrl: first[0].serverUrl
}), "unique");

const tooMany = Array.from({ length: 33 }, (_, index) => ({
  id: `${String(index).padStart(8, "0")}-1111-4111-8111-111111111111`,
  label: `Server ${index}`,
  serverUrl: `https://server-${index}.example.com`
}));
expectError(() => saveServerProfiles(storage, tooMany), "up to 32");

const oversizedProfiles = Array.from({ length: 32 }, (_, index) => ({
  id: `${index.toString(16).padStart(8, "0")}-1111-4111-8111-111111111111`,
  label: `Server ${index}`,
  serverUrl: `https://server-${index}.${Array.from({ length: 110 }, () => "abcdefghij").join(".")}.com`
}));
expectError(() => saveServerProfiles(storage, oversizedProfiles), "32 KiB");

expectError(() => saveServerProfiles(storage, [first[0], { ...first[0], serverUrl: "https://other.example.com" }]),
  "unique");

storedValue = "x".repeat(MAX_SERVER_PROFILE_DOCUMENT_BYTES + 1);
assert(loadServerProfiles(storage).error?.includes("too large") === true,
  "Oversized documents must fail closed without being parsed.");
storedValue = "not json";
assert(loadServerProfiles(storage).error?.includes("invalid") === true,
  "Malformed documents must fail closed.");
clearServerProfiles(storage);
assert(storedValue === null && loadServerProfiles(storage).profiles.length === 0,
  "Invalid saved data must be removable only through an explicit clear.");

const navigation = buildServerNavigationUrl("https://target.example.com", "https://launcher.example.com");
assert(navigation === "https://target.example.com/#tmuxctl-launcher=https%3A%2F%2Flauncher.example.com",
  "Navigation may carry only the normalized launcher origin in its fragment.");
assert(launcherOriginFromHash(new URL(navigation).hash, "https://target.example.com") ===
  "https://launcher.example.com", "The target must recover a valid launcher origin.");
assert(launcherOriginFromHash("#tmuxctl-launcher=http%3A%2F%2Fevil.example.com", "https://target.example.com") === null,
  "Non-HTTPS remote launcher origins must be rejected.");
assert(launcherOriginFromHash("#tmuxctl-launcher=https%3A%2F%2Ftarget.example.com", "https://target.example.com") === null,
  "A target must not offer a return link to itself.");
assert(launcherOriginFromHash("#unrelated=value", "https://target.example.com") === null,
  "Unrelated fragments must not become launcher origins.");

console.log("server profile tests passed");
