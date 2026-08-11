import { filterSessionsByName } from "../src/sessionFilter.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const ordered = [
  { id: "recent", name: "Claude Work" },
  { id: "older", name: "mitmproxy" },
  { id: "last", name: "Build Agent" }
];

assert(filterSessionsByName(ordered, "cla").map((session) => session.id).join() === "recent",
  "Filtering must update from a partial name without submission.");
assert(filterSessionsByName(ordered, "PROXY").map((session) => session.id).join() === "older",
  "Filtering must be case-insensitive.");
assert(filterSessionsByName(ordered, "  agent ").map((session) => session.id).join() === "last",
  "Filtering must ignore surrounding query whitespace.");
assert(filterSessionsByName(ordered, "missing").length === 0,
  "A query with no matches must produce an explicit empty result.");
assert(filterSessionsByName(ordered, "").map((session) => session.id).join() === "recent,older,last",
  "Clearing the query must restore the complete existing order.");
assert(ordered.map((session) => session.id).join() === "recent,older,last",
  "Filtering must not mutate recency order.");

console.log("session filter tests passed");
