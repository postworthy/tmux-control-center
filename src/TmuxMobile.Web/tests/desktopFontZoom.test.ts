import {
  DEFAULT_TERMINAL_FONT_SIZE,
  MAX_TERMINAL_FONT_SIZE,
  MIN_TERMINAL_FONT_SIZE,
  nextTerminalFontSize,
  terminalFontSizeForWheel
} from "../desktop/fontZoom.js";

function assert(condition: boolean, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

assert(nextTerminalFontSize(DEFAULT_TERMINAL_FONT_SIZE, -1) === 15,
  "Wheel up with Ctrl must increase the terminal font by one point.");
assert(nextTerminalFontSize(DEFAULT_TERMINAL_FONT_SIZE, 1) === 13,
  "Wheel down with Ctrl must decrease the terminal font by one point.");
assert(nextTerminalFontSize(DEFAULT_TERMINAL_FONT_SIZE, 0) === DEFAULT_TERMINAL_FONT_SIZE,
  "A zero wheel delta must leave the terminal font unchanged.");
assert(nextTerminalFontSize(MAX_TERMINAL_FONT_SIZE, -100) === MAX_TERMINAL_FONT_SIZE,
  "Terminal font zoom must stop at the upper bound.");
assert(nextTerminalFontSize(MIN_TERMINAL_FONT_SIZE, 100) === MIN_TERMINAL_FONT_SIZE,
  "Terminal font zoom must stop at the lower bound.");
assert(nextTerminalFontSize(Number.NaN, -1) === DEFAULT_TERMINAL_FONT_SIZE + 1,
  "Invalid current sizes must recover from the default before zooming.");
assert(terminalFontSizeForWheel(DEFAULT_TERMINAL_FONT_SIZE, -1, false) === null,
  "An unmodified wheel must remain outside the font zoom path.");
assert(terminalFontSizeForWheel(DEFAULT_TERMINAL_FONT_SIZE, -1, true) === 15,
  "A Ctrl-modified wheel must enter the bounded font zoom path.");

console.log("desktop font zoom tests passed");
