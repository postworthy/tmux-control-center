export interface DesktopShortcutEvent {
  code: string;
  repeat: boolean;
}

export function isNativeFullscreenToggle(
  event: DesktopShortcutEvent, nativeBridgeAvailable: boolean): boolean {
  return nativeBridgeAvailable && event.code === "F12" && !event.repeat;
}
