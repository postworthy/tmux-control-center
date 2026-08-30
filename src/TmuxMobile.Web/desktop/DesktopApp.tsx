import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import DesktopTerminal, { type TerminalConnectionState } from "./DesktopTerminal";
import { reconnectDelay } from "./reconnect";
import {
  UnauthorizedError,
  createSession,
  getTopology,
  getSessions,
  inventoryWebSocketUrl,
  killSession,
  login,
  splitPane,
  type InventorySnapshot,
  type TmuxSession
} from "./desktopApi";
import { activePaneId, sessionIconLabel } from "./desktopNavigation";

interface OpenTab {
  sessionId: string;
  name: string;
}

interface TerminalMenu {
  sessionId: string;
  x: number;
  y: number;
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
  const [inventoryLoaded, setInventoryLoaded] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [terminalMenu, setTerminalMenu] = useState<TerminalMenu | null>(null);
  const deepLinkOpened = useRef(false);
  const newSessionInput = useRef<HTMLInputElement>(null);
  const [nativeProfilesAvailable, setNativeProfilesAvailable] = useState(false);

  const showProfiles = () => {
    const bridge = window.external as unknown as { sendMessage: (message: string) => void };
    bridge.sendMessage(JSON.stringify({ type: "showProfiles" }));
  };

  const openSessionWindow = (sessionId: string) => {
    const bridge = window.external as unknown as { sendMessage: (message: string) => void };
    bridge.sendMessage(JSON.stringify({ type: "openSessionWindow", sessionId }));
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
      setInventoryLoaded(true);
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
        setInventoryLoaded(true);
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

  useEffect(() => {
    if (!inventoryLoaded || !inventoryConnected) return;
    const live = new Set(sessions.map(session => session.id));
    if (activeId && !live.has(activeId))
      setActiveId(tabs.filter(tab => live.has(tab.sessionId)).at(-1)?.sessionId ?? null);
    setTabs(current => {
      const next = current.filter(tab => live.has(tab.sessionId));
      return next.length === current.length ? current : next;
    });
  }, [activeId, inventoryConnected, inventoryLoaded, sessions, tabs]);

  const open = (session: TmuxSession) => {
    setTabs(current => current.some(tab => tab.sessionId === session.id)
      ? current : [...current, { sessionId: session.id, name: session.name }]);
    setActiveId(session.id);
  };

  useEffect(() => {
    if (deepLinkOpened.current || !sessions.length) return;
    const requested = new URLSearchParams(location.search).get("session");
    if (!requested) { deepLinkOpened.current = true; return; }
    const session = sessions.find(item => item.id === requested);
    if (session) open(session);
    deepLinkOpened.current = true;
  }, [sessions]);

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

  useEffect(() => {
    if (!terminalMenu) return;
    const closeMenu = (event: KeyboardEvent) => {
      if (event.key === "Escape") setTerminalMenu(null);
    };
    const closeForResize = () => setTerminalMenu(null);
    window.addEventListener("keydown", closeMenu, true);
    window.addEventListener("resize", closeForResize);
    return () => {
      window.removeEventListener("keydown", closeMenu, true);
      window.removeEventListener("resize", closeForResize);
    };
  }, [terminalMenu]);

  const showTerminalMenu = (sessionId: string, x: number, y: number) => {
    const menuWidth = 210;
    const menuHeight = 84;
    setTerminalMenu({
      sessionId,
      x: Math.max(8, Math.min(x, window.innerWidth - menuWidth - 8)),
      y: Math.max(8, Math.min(y, window.innerHeight - menuHeight - 8))
    });
  };

  const splitActivePane = async (orientation: "horizontal" | "vertical") => {
    const target = terminalMenu;
    setTerminalMenu(null);
    if (!target) return;
    try {
      const paneId = activePaneId(await getTopology(target.sessionId));
      if (!paneId) throw new Error("The active tmux pane is no longer available.");
      await splitPane(paneId, orientation);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not split the active pane");
    }
  };

  useEffect(() => {
    const keydown = (event: KeyboardEvent) => {
      if (!activeId || event.altKey || event.metaKey) return;
      if (event.ctrlKey && event.shiftKey && event.code === "KeyW") {
        event.preventDefault();
        close(activeId);
        return;
      }
      if (event.ctrlKey && (event.code === "PageUp" || event.code === "PageDown")) {
        event.preventDefault();
        const index = tabs.findIndex(tab => tab.sessionId === activeId);
        if (index < 0 || tabs.length < 2) return;
        const offset = event.code === "PageUp" ? -1 : 1;
        setActiveId(tabs[(index + offset + tabs.length) % tabs.length].sessionId);
      }
    };
    window.addEventListener("keydown", keydown, true);
    return () => window.removeEventListener("keydown", keydown, true);
  }, [activeId, tabs]);

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
    const confirmation = window.prompt(
      `Kill tmux session “${session.name}”? This ends every window and pane in it. Type the session name to confirm.`);
    if (confirmation !== session.name) return;
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

  return <main className={sidebarCollapsed ? "desktop-shell sidebar-collapsed" : "desktop-shell"}>
    <aside className="session-sidebar">
      {sidebarCollapsed ? <nav className="sidebar-icon-rail" aria-label="Collapsed sidebar">
        <button title="Show sessions" aria-label="Show sessions" onClick={() => setSidebarCollapsed(false)}>
          <span aria-hidden="true">☰</span>
        </button>
        <button title="Create session" aria-label="Create session" onClick={() => {
          setSidebarCollapsed(false);
          window.requestAnimationFrame(() => newSessionInput.current?.focus());
        }}>
          <span aria-hidden="true">+</span>
        </button>
        <div className="rail-session-list" aria-label="Tmux sessions">
          {sessions.map(session => <button key={session.id}
            className={session.id === activeId ? "rail-session active" : "rail-session"}
            title={`${session.name} — ${session.isAttached ? "attached" : "detached"}`}
            aria-label={`Open ${session.name}`} onClick={() => open(session)}>
            <span className="rail-session-label" aria-hidden="true">{sessionIconLabel(session.name)}</span>
            <span className={session.isAttached ? "rail-status attached" : "rail-status detached"} aria-hidden="true" />
          </button>)}
        </div>
        {nativeProfilesAvailable && <button className="rail-servers" title="Servers" aria-label="Servers" onClick={showProfiles}>
          <span aria-hidden="true">⚙</span>
        </button>}
      </nav> : <>
        <header><div className="sidebar-heading"><div className="brand-mark">tmuxctl</div><span>{location.host}</span></div>
          <button className="sidebar-collapse" title="Collapse sidebar" aria-label="Collapse sidebar"
            onClick={() => setSidebarCollapsed(true)}>‹</button>
          {nativeProfilesAvailable && <button className="servers-button" onClick={showProfiles}>Servers</button>}
        </header>
        <form className="new-session" onSubmit={submitCreate}>
          <input ref={newSessionInput} aria-label="New session name" placeholder="New session" value={newName}
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
      </>}
    </aside>
    <section className="workspace">
      <nav className="tab-strip" aria-label="Open sessions">
        {tabs.map(tab => <div className={tab.sessionId === activeId ? "tab active" : "tab"} key={tab.sessionId}>
          <button onClick={() => setActiveId(tab.sessionId)}>{tab.name}</button>
          {nativeProfilesAvailable && <button className="tab-popout" title={`Open ${tab.name} in a new window`}
            aria-label={`Open ${tab.name} in a new window`} onClick={() => openSessionWindow(tab.sessionId)}>↗</button>}
          <button className="tab-close" aria-label={`Detach ${tab.name}`} onClick={() => close(tab.sessionId)}>×</button>
        </div>)}
        <span className={`connection ${activeTab ? connections[activeTab.sessionId] ?? "connecting" : ""}`}>
          {!inventoryConnected ? "server offline" : activeTab ? connections[activeTab.sessionId] ?? "connecting" : "no session"}
        </span>
      </nav>
      <div className="terminal-stage">
        {tabs.map(tab => <DesktopTerminal key={tab.sessionId} sessionId={tab.sessionId}
          active={tab.sessionId === activeId}
          onConnectionState={state => updateConnection(tab.sessionId, state)}
          onContextMenu={(x, y) => showTerminalMenu(tab.sessionId, x, y)}
          onError={setError} />)}
        {!activeTab && <div className="empty-state"><h1>Select a session</h1><p>Open an existing tmux session from the sidebar.</p></div>}
      </div>
      {terminalMenu && <div className="terminal-menu-layer" onMouseDown={() => setTerminalMenu(null)}>
        <div className="terminal-menu" role="menu" aria-label="Terminal actions"
          style={{ left: terminalMenu.x, top: terminalMenu.y }} onMouseDown={event => event.stopPropagation()}>
          <button role="menuitem" onClick={() => void splitActivePane("horizontal")}>Split left / right</button>
          <button role="menuitem" onClick={() => void splitActivePane("vertical")}>Split top / bottom</button>
        </div>
      </div>}
      {error && <div className="error-banner" role="alert">{error}<button onClick={() => setError(null)}>Dismiss</button></div>}
    </section>
  </main>;
}
