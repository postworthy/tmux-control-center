export const MAX_COPY_BYTES = 128 * 1024;

// OSC 52 carries tmux's copied text. Never answer clipboard queries or treat
// arbitrary terminal output as HTML. Bound the encoded data before decoding.
export function tmuxClipboardText(data: string): string | null {
  const separator = data.indexOf(";");
  if (separator < 0 || !/^[cps01234567]*$/.test(data.slice(0, separator))) return null;
  const encoded = data.slice(separator + 1);
  if (!encoded || encoded.length > Math.ceil(MAX_COPY_BYTES / 3) * 4 ||
      !/^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(encoded)) return null;
  try {
    const bytes = Uint8Array.from(atob(encoded), character => character.charCodeAt(0));
    if (bytes.length > MAX_COPY_BYTES) return null;
    return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
  } catch {
    return null;
  }
}

export interface ClipboardEnvironment {
  copyDuringGesture: (text: string) => boolean;
  writeText?: (text: string) => Promise<void>;
}

export async function copyTerminalText(text: string, environment: ClipboardEnvironment): Promise<void> {
  if (!text) return;
  if (new TextEncoder().encode(text).byteLength > MAX_COPY_BYTES)
    throw new Error("Clipboard copy is larger than 128 KiB. Select a smaller amount of text.");
  // WebKit's async Clipboard API may be unavailable/denied in Photino. Try
  // the user-gesture copy command before awaiting anything and losing activation.
  try {
    if (environment.copyDuringGesture(text)) return;
  } catch { /* Try the asynchronous API when the command is unsupported. */ }
  if (!environment.writeText)
    throw new Error("Clipboard copy is unavailable. Hold Shift while selecting, then press Ctrl+Shift+C.");
  try { await environment.writeText(text); }
  catch {
    throw new Error("Clipboard copy was denied. Hold Shift while selecting, then press Ctrl+Shift+C.");
  }
}

export function browserClipboardEnvironment(): ClipboardEnvironment {
  return {
    copyDuringGesture: text => {
      const focused = document.activeElement;
      const field = document.createElement("textarea");
      field.value = text;
      field.setAttribute("aria-hidden", "true");
      field.style.cssText = "position:fixed;left:-10000px;top:0;opacity:0";
      document.body.appendChild(field);
      try {
        field.focus({ preventScroll: true });
        field.select();
        return document.execCommand("copy");
      } finally {
        field.remove();
        if (focused instanceof HTMLElement) focused.focus({ preventScroll: true });
      }
    },
    writeText: navigator.clipboard?.writeText
      ? text => navigator.clipboard.writeText(text)
      : undefined
  };
}
