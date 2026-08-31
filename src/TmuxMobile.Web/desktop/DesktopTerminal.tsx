import { useEffect, useRef } from "react";
import { Terminal } from "@xterm/xterm";
import { FitAddon } from "@xterm/addon-fit";
import { terminalWebSocketUrl } from "./desktopApi";
import { DEFAULT_TERMINAL_FONT_SIZE, terminalFontSizeForWheel } from "./fontZoom";
import { isTerminalPing, reconnectDelay } from "./reconnect";
import {
  NATIVE_WINDOW_GEOMETRY_EVENT,
  SETTLED_TERMINAL_REFIT_DELAYS,
  TERMINAL_GEOMETRY_POLL_MILLISECONDS,
  terminalHostCanBeFit,
  terminalHostGeometryKey
} from "./terminalLayout";
import { DESKTOP_HISTORY_FLUSH_MILLISECONDS, historyRequestFromWheelDelta } from "./terminalWheel";
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
  const socketRef = useRef<WebSocket | null>(null);
  const refitRef = useRef<(() => void) | null>(null);
  const activeRef = useRef(active);
  const connectionCallbackRef = useRef(onConnectionState);
  const errorCallbackRef = useRef(onError);

  useEffect(() => { connectionCallbackRef.current = onConnectionState; }, [onConnectionState]);
  useEffect(() => { errorCallbackRef.current = onError; }, [onError]);
  activeRef.current = active;

  useEffect(() => {
    if (!active) return;
    refitRef.current?.();
    const frame = window.requestAnimationFrame(() => {
      refitRef.current?.();
      const terminal = terminalRef.current;
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

    const encoder = new TextEncoder();
    let stopped = false;
    let retryAttempt = 0;
    let retryTimer = 0;
    let fitFrame = 0;
    let lastResize = "";
    let lastHostGeometry: string | null = null;
    let historyWheelDelta = 0;
    let historyTimer = 0;
    const settleTimers = new Set<number>();
    const fitAndResize = () => {
      fitFrame = 0;
      const host = hostRef.current;
      if (stopped || !activeRef.current || !host ||
          !terminalHostCanBeFit(host.clientWidth, host.clientHeight)) return;
      lastHostGeometry = terminalHostGeometryKey(host.clientWidth, host.clientHeight);
      fit.fit();
      const socket = socketRef.current;
      if (socket?.readyState === WebSocket.OPEN) {
        const dimensions = `${terminal.cols}x${terminal.rows}`;
        if (dimensions !== lastResize) {
          lastResize = dimensions;
          socket.send(JSON.stringify({ type: "resize", cols: terminal.cols, rows: terminal.rows }));
        }
      }
    };
    const queueFit = () => {
      if (!fitFrame) fitFrame = window.requestAnimationFrame(fitAndResize);
    };
    const scheduleSettledFit = () => {
      queueFit();
      if (settleTimers.size) return;
      for (const delay of SETTLED_TERMINAL_REFIT_DELAYS) {
        const timer = window.setTimeout(() => {
          settleTimers.delete(timer);
          queueFit();
        }, delay);
        settleTimers.add(timer);
      }
    };
    refitRef.current = scheduleSettledFit;
    const checkHostGeometry = () => {
      if (stopped || !activeRef.current) return;
      const host = hostRef.current;
      if (!host) return;
      const geometry = terminalHostGeometryKey(host.clientWidth, host.clientHeight);
      if (geometry === null || geometry === lastHostGeometry) return;
      lastHostGeometry = geometry;
      scheduleSettledFit();
    };
    const flushHistoryWheel = () => {
      historyTimer = 0;
      const request = historyRequestFromWheelDelta(historyWheelDelta);
      historyWheelDelta = 0;
      const socket = socketRef.current;
      if (request && socket?.readyState === WebSocket.OPEN) socket.send(request);
    };
    const terminalWheel = (event: WheelEvent) => {
      const current = terminal.options.fontSize ?? DEFAULT_TERMINAL_FONT_SIZE;
      const next = terminalFontSizeForWheel(current, event.deltaY, event.ctrlKey);
      if (next === null) {
        if (!Number.isFinite(event.deltaY) || event.deltaY === 0) return;
        event.preventDefault();
        event.stopPropagation();
        historyWheelDelta += event.deltaY;
        if (!historyTimer)
          historyTimer = window.setTimeout(flushHistoryWheel, DESKTOP_HISTORY_FLUSH_MILLISECONDS);
        return;
      }
      event.preventDefault();
      event.stopPropagation();
      if (next === current) return;
      terminal.options.fontSize = next;
      scheduleSettledFit();
    };
    const terminalHost = hostRef.current!;
    terminalHost.addEventListener("wheel", terminalWheel, { capture: true, passive: false });
    const suppressBrowserContextMenu = (event: MouseEvent) => event.preventDefault();
    terminalHost.addEventListener("contextmenu", suppressBrowserContextMenu, true);
    const observer = new ResizeObserver(scheduleSettledFit);
    observer.observe(terminalHost);
    if (terminalHost.parentElement) observer.observe(terminalHost.parentElement);
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
        lastResize = "";
        connectionCallbackRef.current("connected");
        scheduleSettledFit();
        if (activeRef.current) terminal.focus();
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
    const viewportChanged = () => scheduleSettledFit();
    const visibilityChanged = () => { if (!document.hidden) scheduleSettledFit(); };
    const geometryTimer = window.setInterval(checkHostGeometry, TERMINAL_GEOMETRY_POLL_MILLISECONDS);
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    window.addEventListener("pagehide", pageHide);
    window.addEventListener("resize", viewportChanged);
    window.addEventListener(NATIVE_WINDOW_GEOMETRY_EVENT, viewportChanged);
    window.visualViewport?.addEventListener("resize", viewportChanged);
    document.addEventListener("fullscreenchange", viewportChanged);
    document.addEventListener("visibilitychange", visibilityChanged);
    scheduleSettledFit();
    connect();

    return () => {
      stopped = true;
      window.clearTimeout(retryTimer);
      window.clearTimeout(historyTimer);
      window.clearInterval(geometryTimer);
      if (fitFrame) window.cancelAnimationFrame(fitFrame);
      for (const timer of settleTimers) window.clearTimeout(timer);
      window.removeEventListener("online", online);
      window.removeEventListener("offline", offline);
      window.removeEventListener("pagehide", pageHide);
      window.removeEventListener("resize", viewportChanged);
      window.removeEventListener(NATIVE_WINDOW_GEOMETRY_EVENT, viewportChanged);
      window.visualViewport?.removeEventListener("resize", viewportChanged);
      document.removeEventListener("fullscreenchange", viewportChanged);
      document.removeEventListener("visibilitychange", visibilityChanged);
      terminalHost.removeEventListener("wheel", terminalWheel, true);
      terminalHost.removeEventListener("contextmenu", suppressBrowserContextMenu, true);
      observer.disconnect();
      input.dispose();
      socketRef.current?.close(1000, "Desktop tab closed");
      socketRef.current = null;
      terminalRef.current = null;
      refitRef.current = null;
      terminal.dispose();
    };
  }, [sessionId]);

  return <div className={active ? "terminal-host active" : "terminal-host"}
    ref={hostRef} aria-label="Terminal" aria-hidden={!active} />;
}
