import { useCallback, useEffect, useRef, useState } from "react";
import { Terminal } from "@xterm/xterm";
import { FitAddon } from "@xterm/addon-fit";
import "@xterm/xterm/css/xterm.css";
import { MAX_PASTE_BYTES, pasteByteLength, requiresPasteConfirmation, serializeTerminalInput } from "./terminalInput";
import {
  classifyTouchAxis,
  consumeTouchScroll,
  historyRequestFromScrollLines,
  serializeHistoryRequest,
  type TerminalHistoryAction,
  type TouchAxis
} from "./terminalScroll";
import type { TmuxSession } from "./types";

interface Props { session: TmuxSession; tmuxPrefix: string; onBack: () => void; }
type ConnectionState = "connecting" | "connected" | "disconnected";

export function TerminalView({ session, tmuxPrefix, onBack }: Props) {
  const container = useRef<HTMLDivElement>(null);
  const terminal = useRef<Terminal | null>(null);
  const fit = useRef<FitAddon | null>(null);
  const socket = useRef<WebSocket | null>(null);
  const [connection, setConnection] = useState<ConnectionState>("connecting");
  const modifiers = useRef({ control: false, alt: false });
  const [modifierState, setModifierState] = useState({ control: false, alt: false });
  const [pasteOpen, setPasteOpen] = useState(false);
  const [pasteDraft, setPasteDraft] = useState("");
  const [pasteStatus, setPasteStatus] = useState("");
  const [historyMode, setHistoryMode] = useState(false);

  const send = useCallback((data: string) => {
    if (socket.current?.readyState !== WebSocket.OPEN) return;
    for (const message of serializeTerminalInput(data)) socket.current.send(message);
  }, []);

  const requestHistory = useCallback((action: TerminalHistoryAction, pages = 1) => {
    if (socket.current?.readyState !== WebSocket.OPEN) return;
    socket.current.send(serializeHistoryRequest(action, pages));
    if (action === "older") setHistoryMode(true);
    if (action === "latest") setHistoryMode(false);
  }, []);

  const connect = useCallback(() => {
    if (socket.current?.readyState === WebSocket.OPEN ||
        socket.current?.readyState === WebSocket.CONNECTING) return;
    setConnection("connecting");
    const protocol = location.protocol === "https:" ? "wss:" : "ws:";
    const ws = new WebSocket(`${protocol}//${location.host}/ws/terminal/${session.id}`);
    ws.binaryType = "arraybuffer";
    socket.current = ws;
    ws.onopen = () => {
      setConnection("connected");
      fit.current?.fit();
      if (terminal.current)
        ws.send(JSON.stringify({ type: "resize", cols: terminal.current.cols, rows: terminal.current.rows }));
      terminal.current?.focus();
    };
    ws.onmessage = (event) => {
      if (event.data instanceof ArrayBuffer) terminal.current?.write(new Uint8Array(event.data));
    };
    ws.onclose = () => {
      if (socket.current === ws) socket.current = null;
      setHistoryMode(false);
      setConnection("disconnected");
    };
    ws.onerror = () => ws.close();
  }, [session.id]);

  useEffect(() => {
    const xterm = new Terminal({
      cursorBlink: true, convertEol: false, scrollback: 2000,
      fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
      fontSize: 14,
      theme: { background: "#06100d", foreground: "#dff9ef", cursor: "#54e0ad", selectionBackground: "#346a58" }
    });
    const fitAddon = new FitAddon();
    xterm.loadAddon(fitAddon);
    xterm.open(container.current!);
    terminal.current = xterm;
    fit.current = fitAddon;
    const input = xterm.onData((value) => {
      let data = value;
      if (modifiers.current.control && value.length === 1) {
        const code = value.toUpperCase().charCodeAt(0);
        if (code >= 64 && code <= 95) data = String.fromCharCode(code & 31);
      }
      if (modifiers.current.alt) data = `\u001b${data}`;
      if (modifiers.current.control || modifiers.current.alt) {
        modifiers.current = { control: false, alt: false };
        setModifierState({ control: false, alt: false });
      }
      send(data);
    });
    const resize = new ResizeObserver(() => {
      fitAddon.fit();
      if (socket.current?.readyState === WebSocket.OPEN)
        socket.current.send(JSON.stringify({ type: "resize", cols: xterm.cols, rows: xterm.rows }));
    });
    resize.observe(container.current!);
    let touch: {
      startX: number;
      startY: number;
      lastY: number;
      axis: TouchAxis;
      remainderPixels: number;
      lines: number;
    } | null = null;
    const touchStart = (event: TouchEvent) => {
      if (event.touches.length !== 1) {
        touch = null;
        return;
      }
      const point = event.touches[0];
      touch = {
        startX: point.clientX,
        startY: point.clientY,
        lastY: point.clientY,
        axis: "pending",
        remainderPixels: 0,
        lines: 0
      };
    };
    const touchMove = (event: TouchEvent) => {
      if (!touch || event.touches.length !== 1) {
        touch = null;
        return;
      }
      const point = event.touches[0];
      if (touch.axis === "pending")
        touch.axis = classifyTouchAxis(point.clientX - touch.startX, point.clientY - touch.startY);
      if (touch.axis !== "vertical") return;

      event.preventDefault();
      const consumed = consumeTouchScroll(touch.remainderPixels, point.clientY - touch.lastY);
      touch.lastY = point.clientY;
      touch.remainderPixels = consumed.remainderPixels;
      touch.lines += consumed.lines;
    };
    const touchEnd = () => {
      if (touch?.axis === "vertical") {
        const message = historyRequestFromScrollLines(touch.lines);
        if (message && socket.current?.readyState === WebSocket.OPEN) {
          socket.current.send(message);
          if (touch.lines < 0) setHistoryMode(true);
        }
      }
      touch = null;
    };
    const touchCancel = () => { touch = null; };
    const terminalViewport = container.current!;
    terminalViewport.addEventListener("touchstart", touchStart, { passive: true });
    terminalViewport.addEventListener("touchmove", touchMove, { passive: false });
    terminalViewport.addEventListener("touchend", touchEnd);
    terminalViewport.addEventListener("touchcancel", touchCancel);
    connect();
    const online = () => connect();
    window.addEventListener("online", online);
    return () => {
      input.dispose();
      resize.disconnect();
      terminalViewport.removeEventListener("touchstart", touchStart);
      terminalViewport.removeEventListener("touchmove", touchMove);
      terminalViewport.removeEventListener("touchend", touchEnd);
      terminalViewport.removeEventListener("touchcancel", touchCancel);
      window.removeEventListener("online", online);
      socket.current?.close();
      socket.current = null;
      xterm.dispose();
      terminal.current = null;
    };
  }, [connect, send]);

  const key = (value: string) => { send(value); terminal.current?.focus(); };
  const toggle = (name: "control" | "alt") => {
    const next = { ...modifiers.current, [name]: !modifiers.current[name] };
    modifiers.current = next;
    setModifierState(next);
    terminal.current?.focus();
  };
  const clearModifiers = () => {
    modifiers.current = { control: false, alt: false };
    setModifierState({ control: false, alt: false });
  };
  const closePaste = () => {
    setPasteOpen(false);
    setPasteDraft("");
    terminal.current?.focus();
  };
  const paste = (value: string) => {
    const bytes = pasteByteLength(value);
    if (!value) {
      setPasteStatus("Clipboard has no text to paste.");
      return;
    }
    if (bytes > MAX_PASTE_BYTES) {
      setPasteStatus("Paste is too large. Limit clipboard text to 128 KiB.");
      return;
    }
    clearModifiers();
    terminal.current?.paste(value);
    setPasteStatus(`Pasted ${value.length.toLocaleString()} characters. Enter was not sent.`);
    closePaste();
  };
  const requestPaste = async () => {
    setPasteStatus("");
    if (!navigator.clipboard?.readText) {
      setPasteOpen(true);
      return;
    }
    try {
      const value = await navigator.clipboard.readText();
      if (!value) {
        setPasteStatus("Clipboard has no text to paste.");
      } else if (pasteByteLength(value) > MAX_PASTE_BYTES) {
        setPasteStatus("Paste is too large. Limit clipboard text to 128 KiB.");
      } else if (requiresPasteConfirmation(value)) {
        setPasteDraft(value);
        setPasteOpen(true);
        setPasteStatus("Review multiline or large clipboard text before sending.");
      } else {
        paste(value);
      }
    } catch {
      setPasteOpen(true);
      setPasteStatus("Clipboard access was unavailable. Paste into the text field manually.");
    }
  };
  const scrollOlder = () => requestHistory("older");
  const scrollLatest = () => requestHistory("latest");
  const leaveTerminal = () => {
    if (historyMode) requestHistory("latest");
    onBack();
  };

  return (
    <main className="terminal-mode">
      <header className="terminal-header">
        <button onClick={leaveTerminal} aria-label={`Return to ${session.name}`}>‹ Sessions</button>
        <strong>{session.name}</strong>
        <span className={`connection connection-${connection}`}>{connection}</span>
      </header>
      {connection === "disconnected" && (
        <div className="reconnect-banner" role="status">
          Terminal disconnected. <button onClick={connect}>Reconnect</button>
        </div>
      )}
      <div className="terminal-viewport" ref={container}
        aria-label={`Interactive terminal for ${session.name}. Swipe down for older output and up for newer output.`} />
      <div className="terminal-controls">
        {pasteStatus && <p className="paste-status" role="status">{pasteStatus}</p>}
        {historyMode && <span className="visually-hidden" role="status">Viewing tmux history.</span>}
        <div className="shortcut-bar" role="toolbar" aria-label="Terminal shortcut keys">
          <button onClick={scrollOlder} aria-label="Scroll one page into older terminal output">Older</button>
          <button onClick={scrollLatest} disabled={!historyMode}
            aria-label="Jump to latest terminal output">Latest</button>
          <button onClick={() => key("\u001b")}>Esc</button>
          <button onClick={() => key("\t")}>Tab</button>
          <button className={modifierState.control ? "active" : ""} aria-pressed={modifierState.control}
            onClick={() => toggle("control")}>Ctrl</button>
          <button className={modifierState.alt ? "active" : ""} aria-pressed={modifierState.alt}
            onClick={() => toggle("alt")}>Alt</button>
          <button aria-label="Arrow up" onClick={() => key("\u001b[A")}>↑</button>
          <button aria-label="Arrow down" onClick={() => key("\u001b[B")}>↓</button>
          <button aria-label="Arrow left" onClick={() => key("\u001b[D")}>←</button>
          <button aria-label="Arrow right" onClick={() => key("\u001b[C")}>→</button>
          <button onClick={() => key("\r")}>Enter</button>
          <button onClick={() => key("\u0003")}>Ctrl-C</button>
          <button onClick={() => key("\u0004")}>Ctrl-D</button>
          <button onClick={() => key(prefixBytes(tmuxPrefix))}>Prefix</button>
          <button onClick={requestPaste} disabled={connection !== "connected"}
            aria-label="Paste clipboard text into terminal">Paste</button>
        </div>
      </div>
      {pasteOpen && (
        <div className="modal-backdrop paste-backdrop">
          <section className="paste-dialog" role="dialog" aria-modal="true" aria-labelledby="paste-title">
            <h2 id="paste-title">Paste into terminal</h2>
            <p>Review this text before sending. Enter will not be added automatically.</p>
            <label htmlFor="terminal-paste">Clipboard text</label>
            <textarea id="terminal-paste" value={pasteDraft} autoFocus rows={7} spellCheck={false}
              autoCapitalize="none" autoCorrect="off"
              onChange={(event) => setPasteDraft(event.target.value)} />
            <div className="paste-actions">
              <button onClick={closePaste}>Cancel</button>
              <button className="primary" onClick={() => paste(pasteDraft)}
                disabled={!pasteDraft || connection !== "connected"}>Send text</button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

function prefixBytes(prefix: string) {
  const match = /^C-([A-Za-z])$/.exec(prefix);
  return match ? String.fromCharCode(match[1].toUpperCase().charCodeAt(0) & 31) : "\u0002";
}
