import { useCallback, useEffect, useRef, useState } from "react";
import { getSessions, UnauthorizedError } from "./api";
import type { InventorySnapshot, TmuxSession } from "./types";

export type LoadState = "loading" | "ready" | "empty" | "error" | "unauthorized";

export function useInventory() {
  const [sessions, setSessions] = useState<TmuxSession[]>([]);
  const [state, setState] = useState<LoadState>("loading");
  const [connected, setConnected] = useState(navigator.onLine);
  const [error, setError] = useState("");
  const reconnect = useRef(0);
  const socket = useRef<WebSocket | null>(null);

  const refresh = useCallback(async () => {
    try {
      const result = await getSessions();
      setSessions(result);
      setState(result.length ? "ready" : "empty");
      setError("");
    } catch (reason) {
      if (reason instanceof UnauthorizedError) setState("unauthorized");
      else {
        setState((current) => current === "ready" ? current : "error");
        setError(reason instanceof Error ? reason.message : "Unable to load sessions");
      }
    }
  }, []);

  useEffect(() => { void refresh(); }, [refresh]);

  useEffect(() => {
    let stopped = false;
    let timer = 0;
    const connect = () => {
      if (stopped || socket.current?.readyState === WebSocket.OPEN) return;
      const protocol = location.protocol === "https:" ? "wss:" : "ws:";
      const ws = new WebSocket(`${protocol}//${location.host}/ws/inventory`);
      socket.current = ws;
      ws.onopen = () => { reconnect.current = 0; setConnected(true); };
      ws.onmessage = (event) => {
        const snapshot = JSON.parse(String(event.data)) as InventorySnapshot;
        setSessions(snapshot.sessions);
        setState(snapshot.sessions.length ? "ready" : "empty");
      };
      ws.onclose = () => {
        if (socket.current === ws) socket.current = null;
        setConnected(false);
        if (!stopped) {
          const delay = Math.min(30_000, 1000 * 2 ** reconnect.current++);
          timer = window.setTimeout(connect, delay);
        }
      };
      ws.onerror = () => ws.close();
    };
    connect();
    const online = () => { setConnected(true); void refresh(); connect(); };
    const offline = () => setConnected(false);
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    return () => {
      stopped = true;
      window.clearTimeout(timer);
      socket.current?.close();
      window.removeEventListener("online", online);
      window.removeEventListener("offline", offline);
    };
  }, [refresh]);

  return { sessions, state, connected, error, refresh };
}
