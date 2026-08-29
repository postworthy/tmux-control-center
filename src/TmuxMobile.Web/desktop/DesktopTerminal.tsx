import { useEffect, useRef } from "react";
import { Terminal } from "@xterm/xterm";
import { FitAddon } from "@xterm/addon-fit";
import { terminalWebSocketUrl } from "./desktopApi";

interface Props {
  sessionId: string;
  onConnectionState: (state: "connecting" | "connected" | "disconnected") => void;
}

export default function DesktopTerminal({ sessionId, onConnectionState }: Props) {
  const hostRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const terminal = new Terminal({
      cursorBlink: true,
      fontFamily: "'JetBrains Mono', 'Ubuntu Mono', 'DejaVu Sans Mono', monospace",
      fontSize: 14,
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
    fit.fit();

    onConnectionState("connecting");
    const socket = new WebSocket(terminalWebSocketUrl(sessionId));
    socket.binaryType = "arraybuffer";
    const encoder = new TextEncoder();
    const resize = () => {
      fit.fit();
      if (socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: "resize", cols: terminal.cols, rows: terminal.rows }));
      }
    };
    const observer = new ResizeObserver(resize);
    observer.observe(hostRef.current!);
    const input = terminal.onData(data => {
      if (socket.readyState === WebSocket.OPEN) socket.send(encoder.encode(data));
    });

    socket.addEventListener("open", () => {
      onConnectionState("connected");
      resize();
      terminal.focus();
    });
    socket.addEventListener("message", event => {
      if (event.data instanceof ArrayBuffer) terminal.write(new Uint8Array(event.data));
      else terminal.write(event.data);
    });
    socket.addEventListener("close", () => onConnectionState("disconnected"));
    socket.addEventListener("error", () => onConnectionState("disconnected"));

    return () => {
      observer.disconnect();
      input.dispose();
      socket.close(1000, "Desktop tab closed");
      terminal.dispose();
    };
  }, [sessionId, onConnectionState]);

  return <div className="terminal-host" ref={hostRef} aria-label="Terminal" />;
}
