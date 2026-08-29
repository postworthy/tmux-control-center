import { useCallback, useEffect, useMemo, useState } from "react";
import DesktopTerminal, { type TerminalConnectionState } from "./DesktopTerminal";
import { reconnectDelay } from "./reconnect";
import {
  UnauthorizedError,
  createSession,
  getSessions,
  inventoryWebSocketUrl,
  killSession,
  login,
  type InventorySnapshot,
  type TmuxSession
} from "./desktopApi";

interface OpenTab {
  sessionId: string;
  name: string;
}

export default function DesktopApp() {
  const [sessions, setSessions] = useState<TmuxSession[]>([]);
  const [tabs, setTabs] = useState<OpenTab[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [authRequired, setAuthRequired] = useState(false);
  const [apiKey, setApiKey] = useState("");
  const [newName, setNewName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [connections, setConnections] = useState<Record<string, TerminalConnectionState>>({});
  const [inventoryConnected, setInventoryConnected] = useState(navigator.onLine);
  const [nativeProfilesAvailable, setNativeProfilesAvailable] = useState(false);

  const showProfiles = () => {
    const bridge = window.external as unknown as { sendMessage: (message: string) => void };
    bridge.sendMessage(JSON.stringify({ type: "showProfiles" }));
  };

  useEffect(() => {
    let announcedReady = false;
    const detect = () => {
      const bridge = window.external as unknown as { sendMessage?: (message: string) => void };
      const available = typeof bridge?.sendMessage === "function";
      setNativeProfilesAvailable(available);
      if (available && !announcedReady) {
        announcedReady = true;
        bridge.sendMessage!(JSON.stringify({ type: "desktopReady" }));
      }
    };
    detect();
    const timer = window.setTimeout(detect, 250);
    return () => window.clearTimeout(timer);
  }, []);

  const refresh = useCallback(async () => {
    try {
      setSessions(await getSessions());
      setAuthRequired(false);
      setError(null);
    } catch (cause) {
      if (cause instanceof UnauthorizedError) setAuthRequired(true);
      else setError(cause instanceof Error ? cause.message : "Could not reach tmuxctl");
    }
  }, []);

  useEffect(() => { void refresh(); }, [refresh]);

  useEffect(() => {
    if (authRequired) return;
    let stopped = false;
    let attempt = 0;
    let timer = 0;
    let socket: WebSocket | null = null;
    const connect = () => {
      if (stopped || socket?.readyState === WebSocket.OPEN || socket?.readyState === WebSocket.CONNECTING) return;
      if (!navigator.onLine) { setInventoryConnected(false); return; }
      socket = new WebSocket(inventoryWebSocketUrl());
      const current = socket;
      current.addEventListener("open", () => { attempt = 0; setInventoryConnected(true); });
      current.addEventListener("message", event => {
        const snapshot = JSON.parse(String(event.data)) as InventorySnapshot;
        setSessions(snapshot.sessions);
      });
      current.addEventListener("close", () => {
        if (socket === current) socket = null;
        setInventoryConnected(false);
        if (!stopped) timer = window.setTimeout(connect, reconnectDelay(attempt++));
      });
      current.addEventListener("error", () => current.close());
    };
    const online = () => { window.clearTimeout(timer); void refresh(); connect(); };
    const offline = () => { window.clearTimeout(timer); setInventoryConnected(false); socket?.close(); };
    window.addEventListener("online", online);
    window.addEventListener("offline", offline);
    connect();
    return () => {
      stopped = true;
      window.clearTimeout(timer);
      window.removeEventListener("online", online);
      window.removeEventListener("offline", offline);
      socket?.close(1000, "Desktop inventory closed");
    };
  }, [authRequired, refresh]);

  const activeTab = useMemo(() => tabs.find(tab => tab.sessionId === activeId) ?? null, [tabs, activeId]);

  const open = (session: TmuxSession) => {
    setTabs(current => current.some(tab => tab.sessionId === session.id)
      ? current : [...current, { sessionId: session.id, name: session.name }]);
    setActiveId(session.id);
  };

  const close = (sessionId: string) => {
    setTabs(current => {
      const index = current.findIndex(tab => tab.sessionId === sessionId);
      const next = current.filter(tab => tab.sessionId !== sessionId);
      if (activeId === sessionId) setActiveId(next[Math.max(0, index - 1)]?.sessionId ?? null);
      return next;
    });
    setConnections(current => {
      const next = { ...current };
      delete next[sessionId];
      return next;
    });
  };

  const updateConnection = useCallback((sessionId: string, state: TerminalConnectionState) => {
    setConnections(current => current[sessionId] === state ? current : { ...current, [sessionId]: state });
  }, []);

  const submitLogin = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      await login(apiKey);
      setApiKey("");
      await refresh();
    } catch {
      setError("Login failed");
    }
  };

  const submitCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!newName.trim()) return;
    try {
      const created = await createSession(newName.trim());
      setNewName("");
      await refresh();
      const session = (await getSessions()).find(item => item.id === created.id);
      if (session) open(session);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not create session");
    }
  };

  const terminate = async (session: TmuxSession) => {
    if (!window.confirm(`Kill tmux session “${session.name}”? This ends every window and pane in it.`)) return;
    try {
      await killSession(session.id);
      close(session.id);
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not kill session");
    }
  };

  if (authRequired) {
    return <main className="login-shell">
      <form className="login-card" onSubmit={submitLogin}>
        <div className="brand-mark">tmuxctl</div>
        <h1>Connect to your tmux server</h1>
        <p>{location.host}</p>
        <label>Login key<input autoFocus type="password" value={apiKey}
          onChange={event => setApiKey(event.target.value)} autoComplete="current-password" /></label>
        <button type="submit">Connect</button>
        {error && <div className="error" role="alert">{error}</div>}
      </form>
    </main>;
  }

  return <main className="desktop-shell">
    <aside className="session-sidebar">
      <header><div className="brand-mark">tmuxctl</div><span>{location.host}</span>
        {nativeProfilesAvailable && <button className="servers-button" onClick={showProfiles}>Servers</button>}
      </header>
      <form className="new-session" onSubmit={submitCreate}>
        <input aria-label="New session name" placeholder="New session" value={newName}
          onChange={event => setNewName(event.target.value)} />
        <button title="Create session" type="submit">+</button>
      </form>
      <div className="session-list" aria-label="Tmux sessions">
        {sessions.map(session => <div className="session-row" key={session.id}>
          <button className="session-open" onClick={() => open(session)}>
            <span className={session.isAttached ? "status attached" : "status detached"} />
            <span><strong>{session.name}</strong><small>{session.windowCount} windows · {session.paneCount} panes</small></span>
          </button>
          <button className="kill" title={`Kill ${session.name}`} onClick={() => void terminate(session)}>×</button>
        </div>)}
      </div>
    </aside>
    <section className="workspace">
      <nav className="tab-strip" aria-label="Open sessions">
        {tabs.map(tab => <div className={tab.sessionId === activeId ? "tab active" : "tab"} key={tab.sessionId}>
          <button onClick={() => setActiveId(tab.sessionId)}>{tab.name}</button>
          <button className="tab-close" aria-label={`Detach ${tab.name}`} onClick={() => close(tab.sessionId)}>×</button>
        </div>)}
        <span className={`connection ${activeTab ? connections[activeTab.sessionId] ?? "connecting" : ""}`}>
          {!inventoryConnected ? "server offline" : activeTab ? connections[activeTab.sessionId] ?? "connecting" : "no session"}
        </span>
      </nav>
      <div className="terminal-stage">
        {tabs.map(tab => <DesktopTerminal key={tab.sessionId} sessionId={tab.sessionId}
          active={tab.sessionId === activeId}
          onConnectionState={state => updateConnection(tab.sessionId, state)} />)}
        {!activeTab && <div className="empty-state"><h1>Select a session</h1><p>Open an existing tmux session from the sidebar.</p></div>}
      </div>
      {error && <div className="error-banner" role="alert">{error}<button onClick={() => setError(null)}>Dismiss</button></div>}
    </section>
  </main>;
}
