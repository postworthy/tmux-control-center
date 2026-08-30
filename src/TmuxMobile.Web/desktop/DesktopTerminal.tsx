import { useEffect, useRef } from "react";
import { Terminal } from "@xterm/xterm";
import { FitAddon } from "@xterm/addon-fit";
import { terminalWebSocketUrl } from "./desktopApi";
import { DEFAULT_TERMINAL_FONT_SIZE, terminalFontSizeForWheel } from "./fontZoom";
import { isTerminalPing, reconnectDelay } from "./reconnect";
import { MAX_PASTE_BYTES, pasteByteLength, requiresPasteConfirmation, serializeTerminalInput } from "../src/terminalInput";

export type TerminalConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

interface Props {
  sessionId: string;
  active: boolean;
  onConnectionState: (state: TerminalConnectionState) => void;
  onError: (message: string) => void;
}

export default function DesktopTerminal({ sessionId, active, onConnectionState, onError }: Props) {
  const hostRef = useRef<HTMLDivElement>(null);
  const terminalRef = useRef<Terminal | null>(null);
  const fitRef = useRef<FitAddon | null>(null);
  const socketRef = useRef<WebSocket | null>(null);
  const connectionCallbackRef = useRef(onConnectionState);
  const errorCallbackRef = useRef(onError);

  useEffect(() => { connectionCallbackRef.current = onConnectionState; }, [onConnectionState]);
  useEffect(() => { errorCallbackRef.current = onError; }, [onError]);

  useEffect(() => {
    if (!active) return;
    const frame = window.requestAnimationFrame(() => {
      fitRef.current?.fit();
      const socket = socketRef.current;
      const terminal = terminalRef.current;
      if (socket?.readyState === WebSocket.OPEN && terminal) {
        socket.send(JSON.stringify({ type: "resize", cols: terminal.cols, rows: terminal.rows }));
      }
      terminal?.focus();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [active]);

  useEffect(() => {
    const terminal = new Terminal({
      cursorBlink: true,
      fontFamily: "'JetBrains Mono', 'Ubuntu Mono', 'DejaVu Sans Mono', monospace",
      fontSize: DEFAULT_TERMINAL_FONT_SIZE,
      scrollback: 10_000,
      allowProposedApi: false,
      theme: {
        background: "#111418",
        foreground: "#e6e8eb",
        cursor: "#72d6a4",
        selectionBackground: "#35586b"
      }
    });
    const fit = new FitAddon();
    terminal.loadAddon(fit);
    terminal.open(hostRef.current!);
    terminalRef.current = terminal;
    fitRef.current = fit;
    fit.fit();

    const encoder = new TextEncoder();
    let stopped = false;
    let retryAttempt = 0;
    let retryTimer = 0;
    const resize = () => {
      if (!hostRef.current?.classList.contains("active")) return;
      fit.fit();
      const socket = socketRef.current;
      if (socket?.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: "resize", cols: terminal.cols, rows: terminal.rows }));
      }
    };
    const zoomWheel = (event: WheelEvent) => {
      const current = terminal.options.fontSize ?? DEFAULT_TERMINAL_FONT_SIZE;
      const next = terminalFontSizeForWheel(current, event.deltaY, event.ctrlKey);
      if (next === null) return;
      event.preventDefault();
      event.stopPropagation();
      if (next === current) return;
      terminal.options.fontSize = next;
      resize();
    };
    const terminalHost = hostRef.current!;
    terminalHost.addEventListener("wheel", zoomWheel, { capture: true, passive: false });
    const observer = new ResizeObserver(resize);
    observer.observe(terminalHost);
    const input = terminal.onData(data => {
      const socket = socketRef.current;
      if (socket?.readyState === WebSocket.OPEN) socket.send(encoder.encode(data));
    });
    terminal.attachCustomKeyEventHandler(event => {
      if (event.type !== "keydown" || !event.ctrlKey || !event.shiftKey) return true;
      if (event.code === "KeyC") {
        const selection = terminal.getSelection();
        if (selection) void navigator.clipboard.writeText(selection)
          .catch(() => errorCallbackRef.current("Clipboard copy was denied by the desktop environment."));
        return false;
      }
      if (event.code === "KeyV") {
        void navigator.clipboard.readText().then(value => {
          if (!value) return;
          if (pasteByteLength(value) > MAX_PASTE_BYTES) {
            errorCallbackRef.current("Clipboard paste is larger than 128 KiB and was not sent.");
            return;
          }
          if (requiresPasteConfirmation(value) &&
              !window.confirm("Paste multiline or large clipboard text into this terminal?")) return;
          const socket = socketRef.current;
          if (socket?.readyState !== WebSocket.OPEN) {
            errorCallbackRef.current("The terminal is disconnected; clipboard text was not sent.");
            return;
          }
          for (const message of serializeTerminalInput(value)) socket.send(message);
        }).catch(() => errorCallbackRef.current("Clipboard paste was denied by the desktop environment."));
        return false;
      }
      return true;
    });

    const connect = () => {
      if (stopped || socketRef.current?.readyState === WebSocket.OPEN ||
          socketRef.current?.readyState === WebSocket.CONNECTING) return;
      if (!navigator.onLine) {
        connectionCallbackRef.current("disconnected");
        return;
      }
      connectionCallbackRef.current(retryAttempt ? "reconnecting" : "connecting");
      const socket = new WebSocket(terminalWebSocketUrl(sessionId));
      socket.binaryType = "arraybuffer";
      socketRef.current = socket;
      socket.addEventListener("open", () => {
        retryAttempt = 0;
        connectionCallbackRef.current("connected");
        resize();
        if (hostRef.current?.classList.contains("active")) terminal.focus();
      });
      socket.addEventListener("message", event => {
        if (event.data instanceof ArrayBuffer) terminal.write(new Uint8Array(event.data));
        else if (isTerminalPing(event.data) && socket.readyState === WebSocket.OPEN)
          socket.send(JSON.stringify({ type: "pong" }));
      });
      socket.addEventListener("close", () => {
        if (socketRef.current === socket) socketRef.current = null;
        if (stopped) return;
        connectionCallbackRef.current("reconnecting");
        retryTimer = window.setTimeout(connect, reconnectDelay(retryAttempt++));
      });
      socket.addEventListener("error", () => socket.close());
    };
    const online = () => {
      window.clearTimeout(retryTimer);
      connect();
    };
    const offline = () => {
      window.clearTimeout(retryTimer);
      connectionCallbackRef.current("disconnected");
      socketRef.current?.close(1001, "Desktop offline");
    };
    const pageHide = () => socketRef.current?.close(1000, "Desktop window closed");
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    window.addEventListener("pagehide", pageHide);
    connect();

    return () => {
      stopped = true;
      window.clearTimeout(retryTimer);
      window.removeEventListener("online", online);
      window.removeEventListener("offline", offline);
      window.removeEventListener("pagehide", pageHide);
      terminalHost.removeEventListener("wheel", zoomWheel, true);
      observer.disconnect();
      input.dispose();
      socketRef.current?.close(1000, "Desktop tab closed");
      socketRef.current = null;
      terminalRef.current = null;
      fitRef.current = null;
      terminal.dispose();
    };
  }, [sessionId]);

  return <div className={active ? "terminal-host active" : "terminal-host"}
    ref={hostRef} aria-label="Terminal" aria-hidden={!active} />;
}
