import { isNativeFullscreenToggle } from "../desktop/desktopShortcuts.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(isNativeFullscreenToggle({ code: "F12", repeat: false }, true),
  "F12 toggles fullscreen in the native desktop shell.");
assert(!isNativeFullscreenToggle({ code: "F12", repeat: true }, true),
  "Holding F12 does not repeatedly toggle fullscreen.");
assert(!isNativeFullscreenToggle({ code: "F11", repeat: false }, true),
  "Other function keys are not captured.");
assert(!isNativeFullscreenToggle({ code: "F12", repeat: false }, false),
  "The server-hosted page does not capture F12 in an ordinary browser.");

console.log("desktop shortcut tests passed");
