import {
  RECONNECT_STABILITY_MILLISECONDS,
  isTerminalPing,
  nextReconnectAttempt,
  reconnectDelay
} from "../desktop/reconnect.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const actualDelays = [-1, 0, 1, 2, 3, 4, 5, 20].map(reconnectDelay);
assert(JSON.stringify(actualDelays) === JSON.stringify(
  [1_000, 1_000, 2_000, 4_000, 8_000, 16_000, 30_000, 30_000]),
"Desktop reconnect delay must use bounded exponential backoff.");

assert(RECONNECT_STABILITY_MILLISECONDS === 10_000,
  "A handshake must remain open before it resets reconnect history.");
assert(JSON.stringify([-1, 0, 1, 4, 5, 20].map(nextReconnectAttempt)) ===
  JSON.stringify([1, 1, 2, 5, 5, 5]),
"Short-lived connections must advance bounded reconnect history.");

assert(isTerminalPing('{"type":"ping"}'), "The terminal heartbeat must be recognized.");
assert(!isTerminalPing('{"type":"output"}') &&
  !isTerminalPing("ordinary terminal text") &&
  !isTerminalPing(new ArrayBuffer(0)),
"Only a JSON heartbeat may be handled as terminal control data.");

console.log("desktop reconnect tests passed");
