import { useCallback, useEffect, useRef, useState } from "react";
import DesktopWorkspace, { type DesktopTab } from "./DesktopWorkspace";
import { type TerminalConnectionState } from "./DesktopTerminal";
import {
  RECONNECT_STABILITY_MILLISECONDS,
  nextReconnectAttempt,
  reconnectDelay
} from "./reconnect";
import {
  UnauthorizedError,
  createSession,
  getSessions,
  inventoryWebSocketUrl,
  killSession,
  login,
  renameSession,
  type InventorySnapshot,
  type TmuxSession
} from "./desktopApi";
import { reconcileDesktopTabs, sessionIconLabel } from "./desktopNavigation";
import {
  activateWorkspaceSession,
  closeWorkspaceSession,
  createWorkspace,
  dropZoneForPoint,
  groupForSession,
  openWorkspaceSession,
  pruneWorkspaceSessions,
  resetWorkspaceLayout,
  splitWorkspaceSessionAtRoot,
  workspaceGroup,
  workspaceGroups,
  type WorkspaceDropZone,
  type WorkspaceGroup,
  type WorkspaceNode
} from "./workspaceLayout";
import { NATIVE_WINDOW_GEOMETRY_EVENT } from "./terminalLayout";

interface PhotinoBridge {
  sendMessage?: (message: string) => void;
  receiveMessage?: (callback: (message: string) => void) => void;
}

export default function DesktopApp() {
  const [sessions, setSessions] = useState<TmuxSession[]>([]);
  const [tabs, setTabs] = useState<DesktopTab[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [layout, setLayout] = useState<WorkspaceNode>(() => createWorkspace("group-0"));
  const [focusedGroupId, setFocusedGroupId] = useState("group-0");
  const [draggingSessionId, setDraggingSessionId] = useState<string | null>(null);
  const [dropTarget, setDropTarget] = useState<WorkspaceDropZone | null>(null);
  const [authRequired, setAuthRequired] = useState(false);
  const [apiKey, setApiKey] = useState("");
  const [newName, setNewName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [connections, setConnections] = useState<Record<string, TerminalConnectionState>>({});
  const [inventoryConnected, setInventoryConnected] = useState(navigator.onLine);
  const [inventoryLoaded, setInventoryLoaded] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [pendingKill, setPendingKill] = useState<TmuxSession | null>(null);
  const [killBusy, setKillBusy] = useState(false);
  const deepLinkOpened = useRef(false);
  const layoutId = useRef(1);
  const newSessionInput = useRef<HTMLInputElement>(null);
  const [nativeProfilesAvailable, setNativeProfilesAvailable] = useState(false);

  const showProfiles = () => {
    const bridge = window.external as unknown as PhotinoBridge;
    bridge.sendMessage?.(JSON.stringify({ type: "showProfiles" }));
  };

  const openSessionWindow = (sessionId: string) => {
    const bridge = window.external as unknown as PhotinoBridge;
    bridge.sendMessage?.(JSON.stringify({ type: "openSessionWindow", sessionId }));
  };

  useEffect(() => {
    let announcedReady = false;
    const detect = () => {
      const bridge = window.external as unknown as PhotinoBridge;
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

  useEffect(() => {
    const bridge = window.external as unknown as PhotinoBridge;
    bridge.receiveMessage?.(message => {
      try {
        const event = JSON.parse(message) as { type?: unknown };
        if (event.type === "nativeWindowGeometryChanged")
          window.dispatchEvent(new Event(NATIVE_WINDOW_GEOMETRY_EVENT));
      } catch { /* Ignore native messages outside this page's protocol. */ }
    });
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
    let stabilityTimer = 0;
    let socket: WebSocket | null = null;
    const connect = () => {
      if (stopped || socket?.readyState === WebSocket.OPEN || socket?.readyState === WebSocket.CONNECTING) return;
      if (!navigator.onLine) { setInventoryConnected(false); return; }
      socket = new WebSocket(inventoryWebSocketUrl());
      const current = socket;
      current.addEventListener("open", () => {
        window.clearTimeout(stabilityTimer);
        stabilityTimer = window.setTimeout(() => {
          stabilityTimer = 0;
          attempt = 0;
        }, RECONNECT_STABILITY_MILLISECONDS);
        setInventoryConnected(true);
      });
      current.addEventListener("message", event => {
        const snapshot = JSON.parse(String(event.data)) as InventorySnapshot;
        setSessions(snapshot.sessions);
        setInventoryLoaded(true);
      });
      current.addEventListener("close", () => {
        if (socket !== current) return;
        window.clearTimeout(stabilityTimer);
        stabilityTimer = 0;
        socket = null;
        setInventoryConnected(false);
        if (!stopped) {
          timer = window.setTimeout(connect, reconnectDelay(attempt));
          attempt = nextReconnectAttempt(attempt);
        }
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
      window.clearTimeout(stabilityTimer);
      window.removeEventListener("online", online);
      window.removeEventListener("offline", offline);
      socket?.close(1000, "Desktop inventory closed");
    };
  }, [authRequired, refresh]);

  useEffect(() => {
    if (!inventoryLoaded || !inventoryConnected) return;
    const live = new Set(sessions.map(session => session.id));
    setLayout(current => {
      const next = pruneWorkspaceSessions(current, live);
      const focused = workspaceGroup(next, focusedGroupId) ?? workspaceGroups(next)[0];
      if (focused.id !== focusedGroupId) setFocusedGroupId(focused.id);
      if (!activeId || !live.has(activeId)) setActiveId(focused.activeId);
      return next;
    });
    setTabs(current => reconcileDesktopTabs(current, sessions));
  }, [activeId, focusedGroupId, inventoryConnected, inventoryLoaded, sessions]);

  const open = (session: TmuxSession) => {
    const groupId = groupForSession(layout, session.id)?.id ??
      workspaceGroup(layout, focusedGroupId)?.id ?? workspaceGroups(layout)[0].id;
    setTabs(current => current.some(tab => tab.sessionId === session.id)
      ? current : [...current, { sessionId: session.id, name: session.name }]);
    setLayout(current => openWorkspaceSession(current, groupId, session.id));
    setFocusedGroupId(groupId);
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
    setTabs(current => current.filter(tab => tab.sessionId !== sessionId));
    setLayout(current => {
      const next = closeWorkspaceSession(current, sessionId);
      const focused = workspaceGroup(next, focusedGroupId) ?? workspaceGroups(next)[0];
      if (focused.id !== focusedGroupId) setFocusedGroupId(focused.id);
      if (activeId === sessionId) setActiveId(focused.activeId);
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

  const focusGroup = (group: WorkspaceGroup) => {
    setFocusedGroupId(group.id);
    setActiveId(group.activeId);
  };

  const activateTab = (groupId: string, sessionId: string) => {
    setLayout(current => activateWorkspaceSession(current, groupId, sessionId));
    setFocusedGroupId(groupId);
    setActiveId(sessionId);
  };

  const dragOverWorkspace = (event: React.DragEvent<HTMLElement>) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    const bounds = event.currentTarget.getBoundingClientRect();
    setDropTarget(dropZoneForPoint(bounds.width, bounds.height,
      event.clientX - bounds.left, event.clientY - bounds.top));
  };

  const dragLeaveWorkspace = (event: React.DragEvent<HTMLElement>) => {
    if (event.relatedTarget instanceof Node && event.currentTarget.contains(event.relatedTarget)) return;
    setDropTarget(null);
  };

  const dropIntoWorkspace = (event: React.DragEvent<HTMLElement>) => {
    event.preventDefault();
    const sessionId = draggingSessionId ?? event.dataTransfer.getData("text/plain");
    const zone = dropTarget ?? "center";
    setDraggingSessionId(null);
    setDropTarget(null);
    if (!tabs.some(tab => tab.sessionId === sessionId)) return;
    const newGroupId = `group-${layoutId.current++}`;
    const next = zone === "center"
      ? resetWorkspaceLayout(layout, newGroupId, sessionId)
      : splitWorkspaceSessionAtRoot(layout, sessionId, zone, newGroupId, `split-${layoutId.current++}`);
    setLayout(next);
    setFocusedGroupId(groupForSession(next, sessionId)?.id ?? focusedGroupId);
    setActiveId(sessionId);
  };

  const resetToSingleView = () => {
    const groupId = `group-${layoutId.current++}`;
    const next = resetWorkspaceLayout(layout, groupId, activeId);
    setLayout(next);
    setFocusedGroupId(groupId);
    setActiveId(next.activeId);
  };

  const layoutIsSplit = workspaceGroups(layout).length > 1;

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
        const group = workspaceGroup(layout, focusedGroupId);
        if (!group || group.tabIds.length < 2) return;
        const index = group.tabIds.indexOf(activeId);
        if (index < 0) return;
        const offset = event.code === "PageUp" ? -1 : 1;
        activateTab(group.id, group.tabIds[(index + offset + group.tabIds.length) % group.tabIds.length]);
      }
    };
    window.addEventListener("keydown", keydown, true);
    return () => window.removeEventListener("keydown", keydown, true);
  }, [activeId, focusedGroupId, layout]);

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

  const terminate = async () => {
    const session = pendingKill;
    if (!session) return;
    setKillBusy(true);
    try {
      await killSession(session.id);
      close(session.id);
      setPendingKill(null);
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not kill session");
    } finally {
      setKillBusy(false);
    }
  };

  const rename = async (session: TmuxSession) => {
    const requested = window.prompt("Rename tmux session", session.name);
    if (requested === null) return;
    const name = requested.trim();
    if (!name || name === session.name) return;
    try {
      await renameSession(session.id, name);
      setTabs(current => current.map(tab => tab.sessionId === session.id ? { ...tab, name } : tab));
      await refresh();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not rename session");
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
        <button title="Single view" aria-label="Reset to single view" disabled={!layoutIsSplit}
          onClick={resetToSingleView}><span aria-hidden="true">▣</span></button>
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
          <div className="sidebar-actions">
            {nativeProfilesAvailable && <button className="servers-button" onClick={showProfiles}>Servers</button>}
            <button className="layout-button" disabled={!layoutIsSplit}
              onClick={resetToSingleView}>Single view</button>
          </div>
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
            <button className="rename" title={`Rename ${session.name}`}
              aria-label={`Rename ${session.name}`} onClick={() => void rename(session)}>✎</button>
            <button className="kill" title={`Kill ${session.name}`}
              onClick={() => setPendingKill(session)}>×</button>
          </div>)}
        </div>
      </>}
    </aside>
    <section className="workspace">
      <DesktopWorkspace layout={layout} tabs={tabs} connections={connections}
        inventoryConnected={inventoryConnected} focusedGroupId={focusedGroupId}
        draggingSessionId={draggingSessionId} dropTarget={dropTarget}
        nativeProfilesAvailable={nativeProfilesAvailable}
        onFocusGroup={focusGroup} onActivate={activateTab} onClose={close}
        onPopout={openSessionWindow} onConnectionState={updateConnection}
        onDragStart={setDraggingSessionId}
        onDragEnd={() => { setDraggingSessionId(null); setDropTarget(null); }}
        onDragOver={dragOverWorkspace} onDragLeave={dragLeaveWorkspace} onDrop={dropIntoWorkspace}
        onError={setError} />
      {error && <div className="error-banner" role="alert">{error}<button onClick={() => setError(null)}>Dismiss</button></div>}
      {pendingKill && <div className="desktop-modal-backdrop" role="presentation"
        onMouseDown={() => { if (!killBusy) setPendingKill(null); }}>
        <section className="desktop-confirm-dialog" role="dialog" aria-modal="true"
          aria-labelledby="desktop-kill-title" onMouseDown={event => event.stopPropagation()}>
          <h2 id="desktop-kill-title">Kill “{pendingKill.name}”?</h2>
          <p>This ends every window and pane in this tmux session.</p>
          <div className="desktop-confirm-actions">
            <button autoFocus disabled={killBusy} onClick={() => setPendingKill(null)}>Cancel</button>
            <button className="danger-button" disabled={killBusy} onClick={() => void terminate()}>
              {killBusy ? "Killing…" : "Kill session"}
            </button>
          </div>
        </section>
      </div>}
    </section>
  </main>;
}
