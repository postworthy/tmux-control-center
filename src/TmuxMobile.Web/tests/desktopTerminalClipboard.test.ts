import {
  MAX_COPY_BYTES, copyTerminalText, tmuxClipboardText
} from "../desktop/terminalClipboard.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}
const sentinel = "tmux clipboard\nλ 😀";
const encoded = btoa(String.fromCharCode(...new TextEncoder().encode(sentinel)));
assert(tmuxClipboardText(`c;${encoded}`) === sentinel, "Decode tmux Unicode and multiline text.");
assert(tmuxClipboardText(`;${encoded}`) === sentinel, "Support the default clipboard target.");
assert(tmuxClipboardText(`p;${encoded}`) === sentinel, "Promote tmux primary selection to the system clipboard.");
for (const input of ["c;?", "c;", "c;invalid!", "x;YQ==", "c;/w==", "YQ==", "c;YQ==;Yg=="])
  assert(tmuxClipboardText(input) === null, "Ignore queries, empty, invalid targets/base64/UTF-8.");
assert(tmuxClipboardText(`c;${btoa("a".repeat(MAX_COPY_BYTES))}`)?.length === MAX_COPY_BYTES,
  "Accept the copy limit.");
assert(tmuxClipboardText(`c;${btoa("a".repeat(MAX_COPY_BYTES + 1))}`) === null,
  "Reject payloads over the decoded limit.");

const writes: string[] = [];
await copyTerminalText(sentinel, {
  copyDuringGesture: text => { writes.push(text); return true; },
  writeText: async () => { throw new Error("Async API must not run after a successful command."); }
});
assert(writes.length === 1 && writes[0] === sentinel, "Copy through the synchronous WebKit path.");
await copyTerminalText(sentinel, {
  copyDuringGesture: () => false,
  writeText: async text => { writes.push(text); }
});
assert(writes[1] === sentinel, "Fall back to the async system clipboard.");
await copyTerminalText(sentinel, {
  copyDuringGesture: () => { throw new Error("Unsupported command"); },
  writeText: async text => { writes.push(text); }
});
assert(writes[2] === sentinel, "Fall back when execCommand throws.");
await copyTerminalText("", { copyDuringGesture: () => { throw new Error("Must not clear clipboard"); } });
for (const environment of [
  { copyDuringGesture: () => false },
  { copyDuringGesture: () => false, writeText: async () => { throw new Error("Denied"); } }
]) {
  let failed = false;
  try { await copyTerminalText(sentinel, environment); }
  catch (error) { failed = error instanceof Error && error.message.includes("Ctrl+Shift+C"); }
  assert(failed, "Missing and denied APIs must report an actionable copy failure.");
}
let oversizedFailed = false;
try {
  await copyTerminalText("😀".repeat(MAX_COPY_BYTES / 4 + 1), {
    copyDuringGesture: () => { throw new Error("Must reject before touching clipboard"); }
  });
} catch (error) { oversizedFailed = error instanceof Error && error.message.includes("128 KiB"); }
assert(oversizedFailed, "Bound copy by UTF-8 bytes before writing.");
console.log("desktop terminal clipboard tests passed");
