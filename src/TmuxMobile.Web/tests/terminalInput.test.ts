import {
  MAX_PASTE_BYTES,
  pasteByteLength,
  requiresPasteConfirmation,
  serializeTerminalInput
} from "../src/terminalInput.js";

const encoder = new TextEncoder();

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

function reconstruct(messages: string[]): string {
  return messages.map((message) => {
    const parsed = JSON.parse(message) as { type: string; data: string };
    assert(parsed.type === "input", "Every terminal message must retain the input type.");
    return parsed.data;
  }).join("");
}

const singleLine = "echo hello";
const singleMessages = serializeTerminalInput(singleLine);
assert(singleMessages.length === 1, "A short line should use one message.");
assert(reconstruct(singleMessages) === singleLine, "A short line must round-trip exactly.");

const unicodeAndEscapes = "🙂\"\\\n\t\u0000é".repeat(4_000);
const chunkedMessages = serializeTerminalInput(unicodeAndEscapes);
assert(chunkedMessages.length > 1, "Large terminal input should be chunked.");
assert(reconstruct(chunkedMessages) === unicodeAndEscapes,
  "Chunking must preserve Unicode and JSON-escaped characters exactly.");
assert(chunkedMessages.every((message) => encoder.encode(message).byteLength <= 12_000),
  "Every serialized message must remain at or below 12,000 bytes.");

assert(pasteByteLength("🙂") === 4, "Paste limits must count UTF-8 bytes.");
assert(requiresPasteConfirmation("first\nsecond"), "Multiline paste must require confirmation.");
assert(requiresPasteConfirmation("x".repeat(1_025)), "Large paste must require confirmation.");
assert(!requiresPasteConfirmation("safe single line"), "A short single line should paste directly.");
assert(MAX_PASTE_BYTES === 131_072, "The total paste limit must remain 128 KiB.");

console.log(`terminal input tests passed (${chunkedMessages.length} bounded chunks)`);
