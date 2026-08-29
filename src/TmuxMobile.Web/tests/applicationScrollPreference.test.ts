import {
  APPLICATION_SCROLL_SESSION_KEY,
  isApplicationScrollEnabled,
  MAX_APPLICATION_SCROLL_SESSIONS,
  parseApplicationScrollSessionIds,
  readApplicationScrollSessionIds,
  writeApplicationScrollPreference
} from "../src/applicationScrollPreference.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

let storedValue: string | null = null;
const storage = {
  getItem: (key: string) => key === APPLICATION_SCROLL_SESSION_KEY ? storedValue : null,
  setItem: (key: string, value: string) => {
    if (key === APPLICATION_SCROLL_SESSION_KEY) storedValue = value;
  }
};

assert(!isApplicationScrollEnabled(storage, "session-a") &&
  !isApplicationScrollEnabled(storage, "session-b"),
"Never-enabled sessions must default off independently.");

writeApplicationScrollPreference(storage, "session-a", true);
assert(isApplicationScrollEnabled(storage, "session-a"),
  "An enabled session must remain enabled across a fresh storage read.");
assert(!isApplicationScrollEnabled(storage, "session-b"),
  "Enabling one session must not enable another session.");

writeApplicationScrollPreference(storage, "session-b", true);
assert(isApplicationScrollEnabled(storage, "session-a") &&
  isApplicationScrollEnabled(storage, "session-b"),
"Sessions must retain independent enabled preferences.");

writeApplicationScrollPreference(storage, "session-a", false);
assert(!isApplicationScrollEnabled(storage, "session-a") &&
  isApplicationScrollEnabled(storage, "session-b"),
"Disabling one session must preserve every other session preference.");

assert(JSON.stringify(parseApplicationScrollSessionIds('["session-b",3,"","session-b","session-c"]')) ===
  '["session-b","session-c"]',
"Stored preferences must accept only unique non-empty session IDs.");
assert(parseApplicationScrollSessionIds("not json").length === 0 &&
  parseApplicationScrollSessionIds('{"session-a":true}').length === 0,
"Malformed or legacy-shaped storage must safely default off.");

const oversized = Array.from({ length: MAX_APPLICATION_SCROLL_SESSIONS + 20 }, (_, index) => `s-${index}`);
assert(parseApplicationScrollSessionIds(JSON.stringify(oversized)).length === MAX_APPLICATION_SCROLL_SESSIONS,
  "Stored enabled-session IDs must remain bounded.");
for (const id of oversized) writeApplicationScrollPreference(storage, id, true);
assert(readApplicationScrollSessionIds(storage).length === MAX_APPLICATION_SCROLL_SESSIONS,
  "Repeated enablement must not grow stored preferences beyond the bound.");
assert(isApplicationScrollEnabled(storage, oversized.at(-1)!),
  "The most recently enabled session must survive bounded eviction.");

const unavailableStorage = {
  getItem: () => { throw new Error("unavailable"); },
  setItem: () => { throw new Error("unavailable"); }
};
assert(!isApplicationScrollEnabled(unavailableStorage, "session-a"),
  "Unavailable storage must default a session off.");
writeApplicationScrollPreference(unavailableStorage, "session-a", true);

console.log("application scroll preference tests passed");
