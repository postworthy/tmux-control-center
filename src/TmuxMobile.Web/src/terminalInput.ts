const INPUT_MESSAGE_LIMIT_BYTES = 12_000;
const INPUT_ENVELOPE_BYTES = utf8Length(JSON.stringify({ type: "input", data: "" }));

export const MAX_PASTE_BYTES = 131_072;
export const PASTE_CONFIRM_BYTES = 1_024;

export function serializeTerminalInput(data: string): string[] {
  if (!data) return [];

  const messages: string[] = [];
  let chunk = "";
  let chunkBytes = INPUT_ENVELOPE_BYTES;

  for (const character of data) {
    const characterBytes = jsonStringCharacterBytes(character);
    if (chunk && chunkBytes + characterBytes > INPUT_MESSAGE_LIMIT_BYTES) {
      messages.push(JSON.stringify({ type: "input", data: chunk }));
      chunk = "";
      chunkBytes = INPUT_ENVELOPE_BYTES;
    }
    chunk += character;
    chunkBytes += characterBytes;
  }

  if (chunk) messages.push(JSON.stringify({ type: "input", data: chunk }));
  return messages;
}

export function pasteByteLength(data: string): number {
  return utf8Length(data);
}

export function requiresPasteConfirmation(data: string): boolean {
  return /[\r\n]/.test(data) || pasteByteLength(data) > PASTE_CONFIRM_BYTES;
}

function jsonStringCharacterBytes(character: string): number {
  if (character === "\"" || character === "\\") return 2;
  if (character === "\b" || character === "\f" || character === "\n" ||
      character === "\r" || character === "\t") return 2;

  const codePoint = character.codePointAt(0)!;
  if (codePoint <= 0x1f || (character.length === 1 && codePoint >= 0xd800 && codePoint <= 0xdfff))
    return 6;
  if (codePoint <= 0x7f) return 1;
  if (codePoint <= 0x7ff) return 2;
  if (codePoint <= 0xffff) return 3;
  return 4;
}

function utf8Length(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}
