import {
  orderSessionsByRecency,
  parseSessionRecency,
  promoteSessionRecency,
  pruneSessionRecency,
  readSessionRecency,
  SESSION_RECENCY_KEY,
  writeSessionRecency
} from "../src/sessionRecency.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const serverSessions = [{ id: "alpha" }, { id: "beta" }, { id: "gamma" }];

assert(JSON.stringify(promoteSessionRecency([], "beta")) === '["beta"]',
  "Opening a terminal must create an MRU entry.");
assert(JSON.stringify(promoteSessionRecency(["beta", "alpha"], "alpha")) === '["alpha","beta"]',
  "Opening a different terminal must promote it to the front.");
assert(JSON.stringify(promoteSessionRecency(["alpha", "beta", "alpha"], "alpha")) === '["alpha","beta"]',
  "Promoting a session must not retain duplicate entries.");

const ordered = orderSessionsByRecency(serverSessions, ["gamma", "alpha"]);
assert(ordered.map((session) => session.id).join(",") === "gamma,alpha,beta",
  "Ranked sessions must lead while untouched sessions retain server order.");
assert(serverSessions.map((session) => session.id).join(",") === "alpha,beta,gamma",
  "Ordering must not mutate the inventory snapshot.");

const staleOrdered = orderSessionsByRecency(serverSessions, ["missing", "beta", "beta"]);
assert(staleOrdered.map((session) => session.id).join(",") === "beta,alpha,gamma",
  "Stale and duplicate IDs must never hide or duplicate live sessions.");
assert(JSON.stringify(pruneSessionRecency(serverSessions, ["missing", "beta", "beta", "alpha"])) ===
  '["beta","alpha"]', "Pruning must retain only unique live IDs in recency order.");

assert(JSON.stringify(parseSessionRecency('["beta",3,"","beta","alpha"]')) === '["beta","alpha"]',
  "Stored recency must accept only unique non-empty string IDs.");
assert(parseSessionRecency("not json").length === 0 && parseSessionRecency('{"id":"alpha"}').length === 0,
  "Malformed or non-array storage must fall back to server order.");

let storedValue: string | null = '["gamma","alpha"]';
const storage = {
  getItem: (key: string) => key === SESSION_RECENCY_KEY ? storedValue : null,
  setItem: (key: string, value: string) => { if (key === SESSION_RECENCY_KEY) storedValue = value; }
};
assert(readSessionRecency(storage).join(",") === "gamma,alpha",
  "Valid device-local recency must survive reload.");
writeSessionRecency(storage, ["beta", "beta", "alpha"]);
assert(storedValue === '["beta","alpha"]', "Stored recency must be deduplicated.");

const unavailableStorage = {
  getItem: () => { throw new Error("unavailable"); },
  setItem: () => { throw new Error("unavailable"); }
};
assert(readSessionRecency(unavailableStorage).length === 0,
  "Unavailable storage must fall back safely.");
writeSessionRecency(unavailableStorage, ["alpha"]);

console.log("session recency tests passed");
