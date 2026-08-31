import { lazy, Suspense, useEffect, useMemo, useRef, useState } from "react";
import {
  createSession,
  getClientConfig,
  getWorkspaceRecoveryStatus,
  login,
  requestWorkspaceRestore
} from "./api";
import { SessionCard } from "./SessionCard";
import { ServerChooser } from "./ServerChooser";
import { filterSessions } from "./sessionFilter";
import {
  orderSessionsByRecency,
  promoteSessionRecency,
  pruneSessionRecency,
  readSessionRecency,
  writeSessionRecency
} from "./sessionRecency";
import { useInventory } from "./useInventory";
import type { WorkspaceRecoveryStatus } from "./types";
import {
  buildServerNavigationUrl,
  launcherOriginFromHash,
  readLauncherOrigin,
  storeLauncherOrigin
} from "./serverProfiles";

const ACTIVE_KEY = "tmux-mobile-active-session";
const TerminalView = lazy(() => import("./TerminalView").then((module) => ({ default: module.TerminalView })));

export default function App() {
  const inventory = useInventory();
  const deck = useRef<HTMLDivElement>(null);
  const [activeId, setActiveId] = useState(() => localStorage.getItem(ACTIVE_KEY) ?? "");
  const [terminalTarget, setTerminalTarget] = useState<{ id: string; name: string } | null>(null);
  const [apiKey, setApiKey] = useState("");
  const [loginError, setLoginError] = useState("");
  const [updateReady, setUpdateReady] = useState<ServiceWorker | null>(null);
  const [tmuxPrefix, setTmuxPrefix] = useState("C-b");
  const [recentSessionIds, setRecentSessionIds] = useState(() => readSessionRecency(localStorage));
  const [searchQuery, setSearchQuery] = useState("");
  const [detachedOnly, setDetachedOnly] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [createName, setCreateName] = useState("");
  const [createError, setCreateError] = useState("");
  const [creating, setCreating] = useState(false);
  const [recoveryStatus, setRecoveryStatus] = useState<WorkspaceRecoveryStatus | null>(null);
  const [restoreBusy, setRestoreBusy] = useState(false);
  const [restoreError, setRestoreError] = useState("");
  const [serversOpen, setServersOpen] = useState(false);
  const [launcherOrigin, setLauncherOrigin] = useState(() => readLauncherOrigin(sessionStorage, location.origin));
  const orderedSessions = useMemo(
    () => orderSessionsByRecency(inventory.sessions, recentSessionIds),
    [inventory.sessions, recentSessionIds]
  );
  const visibleSessions = useMemo(
    () => filterSessions(orderedSessions, searchQuery, detachedOnly),
    [orderedSessions, searchQuery, detachedOnly]
  );

  useEffect(() => {
    if (!location.hash.startsWith("#tmuxctl-launcher=")) return;
    const launcher = launcherOriginFromHash(location.hash, location.origin);
    history.replaceState(history.state, "", `${location.pathname}${location.search}`);
    if (!launcher) return;
    try {
      storeLauncherOrigin(sessionStorage, launcher);
      setLauncherOrigin(launcher);
    } catch {
      // Browsers can disable session storage; the chooser still works without a return shortcut.
    }
  }, []);

  useEffect(() => {
    if (!visibleSessions.length) return;
    const valid = visibleSessions.some((session) => session.id === activeId);
    const next = valid ? activeId : visibleSessions[0].id;
    if (next !== activeId) setActiveId(next);
    requestAnimationFrame(() => {
      deck.current?.querySelector(`[data-session-id="${CSS.escape(next)}"]`)
        ?.scrollIntoView({ block: "start" });
    });
  }, [visibleSessions.map((session) => session.id).join("|")]);

  useEffect(() => {
    if (inventory.state !== "ready" && inventory.state !== "empty") return;
    setRecentSessionIds((current) => {
      const next = pruneSessionRecency(inventory.sessions, current);
      if (next.length === current.length && next.every((id, index) => id === current[index])) return current;
      writeSessionRecency(localStorage, next);
      return next;
    });
  }, [inventory.state, inventory.sessions.map((session) => session.id).join("|")]);

  useEffect(() => {
    if (inventory.state === "ready") void getClientConfig().then((config) => setTmuxPrefix(config.tmuxPrefix));
  }, [inventory.state]);

  useEffect(() => {
    if (inventory.state !== "ready" && inventory.state !== "empty") return;
    let cancelled = false;
    let timer: number | undefined;
    const refreshRecovery = async () => {
      try {
        const status = await getWorkspaceRecoveryStatus();
        if (cancelled) return;
        setRecoveryStatus(status);
        if (status.requestPending && inventory.state === "empty")
          timer = window.setTimeout(() => void refreshRecovery(), 1500);
      } catch {
        if (!cancelled) setRecoveryStatus(null);
      }
    };
    void refreshRecovery();
    return () => {
      cancelled = true;
      if (timer !== undefined) window.clearTimeout(timer);
    };
  }, [inventory.state, recoveryStatus?.requestPending]);

  useEffect(() => {
    if (!activeId) return;
    localStorage.setItem(ACTIVE_KEY, activeId);
  }, [activeId]);

  useEffect(() => {
    if (!("serviceWorker" in navigator)) return;
    void navigator.serviceWorker.register("/service-worker.js").then((registration) => {
      if (registration.waiting) setUpdateReady(registration.waiting);
      registration.addEventListener("updatefound", () => {
        registration.installing?.addEventListener("statechange", () => {
          if (registration.waiting) setUpdateReady(registration.waiting);
        });
      });
    });
    const reload = () => location.reload();
    navigator.serviceWorker.addEventListener("controllerchange", reload);
    return () => navigator.serviceWorker.removeEventListener("controllerchange", reload);
  }, []);

  const scrollTo = (index: number) => {
    const session = visibleSessions[index];
    if (!session) return;
    deck.current?.querySelector(`[data-session-id="${CSS.escape(session.id)}"]`)
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const openTerminal = (session: { id: string; name: string }) => {
    setRecentSessionIds((current) => {
      const next = promoteSessionRecency(current, session.id);
      writeSessionRecency(localStorage, next);
      return next;
    });
    setActiveId(session.id);
    setTerminalTarget(session);
  };

  const submitCreate = async () => {
    setCreating(true);
    setCreateError("");
    try {
      const created = await createSession(createName);
      setCreateOpen(false);
      setCreateName("");
      openTerminal(created);
      void inventory.refresh();
    } catch (reason) {
      setCreateError(reason instanceof Error ? reason.message : "Unable to create session");
    } finally {
      setCreating(false);
    }
  };

  const restoreWorkspace = async () => {
    setRestoreBusy(true);
    setRestoreError("");
    try {
      await requestWorkspaceRestore();
      setRecoveryStatus((current) => current
        ? { ...current, requestPending: true, state: "requested" }
        : current);
      window.setTimeout(() => void inventory.refresh(), 1500);
    } catch (reason) {
      setRestoreError(reason instanceof Error ? reason.message : "Unable to request workspace restore");
    } finally {
      setRestoreBusy(false);
    }
  };

  const activeIndex = Math.max(0, visibleSessions.findIndex((session) => session.id === activeId));
  const openServer = (serverUrl: string) => {
    if (serverUrl === location.origin) {
      setServersOpen(false);
      return;
    }
    location.assign(buildServerNavigationUrl(serverUrl, launcherOrigin ?? location.origin));
  };

  if (serversOpen) return (
    <ServerChooser currentOrigin={location.origin} launcherOrigin={launcherOrigin}
      onClose={() => setServersOpen(false)} onOpenServer={openServer}
      onReturnToLauncher={() => launcherOrigin && location.assign(launcherOrigin)} />
  );

  if (terminalTarget) return (
    <Suspense fallback={<State title="Opening terminal…" busy />}>
      <TerminalView session={terminalTarget} tmuxPrefix={tmuxPrefix} onBack={() => setTerminalTarget(null)} />
    </Suspense>
  );

  if (inventory.state === "unauthorized") {
    return (
      <main className="center-state">
        <form className="login-card" onSubmit={async (event) => {
          event.preventDefault();
          setLoginError("");
          try { await login(apiKey); setApiKey(""); await inventory.refresh(); }
          catch { setLoginError("That access key was not accepted."); }
        }}>
          <span className="brand-mark" aria-hidden="true">&gt;_</span>
          <h1>Tmux Mobile</h1>
          <p>Sign in to your private control service.</p>
          <label htmlFor="api-key">Access key</label>
          <input id="api-key" type="password" value={apiKey} required autoComplete="current-password"
            onChange={(event) => setApiKey(event.target.value)} />
          {loginError && <p className="error-text" role="alert">{loginError}</p>}
          <button type="submit">Sign in</button>
          <button className="secondary-button" type="button" onClick={() => setServersOpen(true)}>Servers</button>
        </form>
      </main>
    );
  }
  if (inventory.state === "loading") return <State title="Finding tmux sessions…" busy
    action={<button onClick={() => setServersOpen(true)}>Servers</button>} />;
  if (inventory.state === "error") return <State title="Unable to load sessions" detail={inventory.error}
    action={<div className="state-actions"><button onClick={inventory.refresh}>Try again</button>
      <button onClick={() => setServersOpen(true)}>Servers</button></div>} />;

  const hasAlerts = !inventory.connected || updateReady;
  const alertCount = Number(!inventory.connected) + Number(Boolean(updateReady));

  return (
    <>
      {hasAlerts && <div className="alerts">
        {!inventory.connected && <div className="offline-banner" role="status">Offline — showing last live state</div>}
        {updateReady && (
          <div className="update-banner" role="status">
            <span>Update ready. Close active terminals before applying.</span>
            <button onClick={() => updateReady.postMessage({ type: "SKIP_WAITING" })}>Apply</button>
          </div>
        )}
      </div>}
      <header className={`session-toolbar ${alertCount ? `with-${alertCount}-alerts` : ""}`}>
        <label className="visually-hidden" htmlFor="session-search">Search sessions by name</label>
        <input id="session-search" type="search" value={searchQuery} placeholder="Search sessions"
          autoComplete="off" autoCapitalize="none" spellCheck={false}
          onChange={(event) => setSearchQuery(event.target.value)} />
        <button className={`detached-filter-button${detachedOnly ? " active" : ""}`}
          aria-pressed={detachedOnly} onClick={() => setDetachedOnly((current) => !current)}>
          Detached
        </button>
        <button className="servers-button" onClick={() => setServersOpen(true)}>Servers</button>
        <button className="new-session-button" onClick={() => {
          setCreateError("");
          setCreateOpen(true);
        }} disabled={!inventory.connected}>+ New</button>
      </header>
      <main className={`session-deck has-toolbar ${hasAlerts ? "has-alerts" : ""} ${alertCount === 2 ? "with-2-alerts" : ""}`}
        ref={deck} onScroll={() => {
        const cards = Array.from(deck.current?.querySelectorAll<HTMLElement>(".session-card") ?? []);
        const current = cards.reduce((best, card) =>
          Math.abs(card.getBoundingClientRect().top) < Math.abs(best.getBoundingClientRect().top) ? card : best,
          cards[0]);
        if (current?.dataset.sessionId) setActiveId(current.dataset.sessionId);
      }}>
        {visibleSessions.map((session, index) => (
          <SessionCard key={session.id} session={session} index={index} total={visibleSessions.length}
            onTerminal={() => openTerminal(session)}
            onRefresh={inventory.refresh}
            onPrevious={() => scrollTo(index - 1)}
            onNext={() => scrollTo(index + 1)} />
        ))}
        {!visibleSessions.length && (
          <section className="deck-empty" aria-live="polite">
            <h1>{emptyDeckTitle(inventory.state === "empty", detachedOnly, searchQuery)}</h1>
            <p>{emptyDeckDetail(inventory.state === "empty", detachedOnly, searchQuery)}</p>
            {inventory.state === "empty" && (
              <div className="empty-actions">
                {recoveryStatus?.enabled && recoveryStatus.snapshotAvailable && (
                  <button className="terminal-button" onClick={() => void restoreWorkspace()}
                    disabled={restoreBusy || recoveryStatus.requestPending}>
                    {restoreBusy || recoveryStatus.requestPending ? "Restore requested…" : "Restore saved workspace"}
                  </button>
                )}
                <button onClick={inventory.refresh}>Refresh</button>
              </div>
            )}
            {recoveryStatus?.snapshotSavedAt && inventory.state === "empty" && (
              <small>Saved {new Date(recoveryStatus.snapshotSavedAt).toLocaleString()}</small>
            )}
            {restoreError && <p className="error-text" role="alert">{restoreError}</p>}
            {recoveryStatus && recoveryStatus.state !== "idle" && recoveryStatus.state !== "requested" &&
              recoveryStatus.state !== "no-snapshot" && recoveryStatus.state !== "restored" && (
              <p className="error-text" role="alert">{recoveryStateMessage(recoveryStatus.state)}</p>
            )}
          </section>
        )}
      </main>
      <div className="session-rail" aria-hidden="true">
        {visibleSessions.map((session, index) =>
          <span key={session.id} className={index === activeIndex ? "active" : ""} />)}
      </div>
      {createOpen && (
        <div className="modal-backdrop" role="presentation" onClick={() => !creating && setCreateOpen(false)}>
          <form className="create-dialog" role="dialog" aria-modal="true" aria-labelledby="create-title"
            onClick={(event) => event.stopPropagation()} onSubmit={(event) => {
              event.preventDefault();
              void submitCreate();
            }}>
            <h2 id="create-title">New tmux session</h2>
            <p>Create a detached session and open its terminal immediately.</p>
            <label htmlFor="session-name">Session name</label>
            <input id="session-name" value={createName} required maxLength={64} autoFocus
              autoComplete="off" autoCapitalize="none" spellCheck={false}
              onChange={(event) => setCreateName(event.target.value)} />
            {createError && <p className="error-text" role="alert">{createError}</p>}
            <div className="create-actions">
              <button type="button" onClick={() => setCreateOpen(false)} disabled={creating}>Cancel</button>
              <button className="terminal-button" type="submit" disabled={creating || !createName.trim()}>
                {creating ? "Creating…" : "Create & open"}
              </button>
            </div>
          </form>
        </div>
      )}
    </>
  );
}

function State({ title, detail, busy, action }: {
  title: string; detail?: string; busy?: boolean; action?: React.ReactNode
}) {
  return (
    <main className="center-state">
      {busy && <span className="spinner" aria-hidden="true" />}
      <h1>{title}</h1>
      {detail && <p>{detail}</p>}
      {action}
    </main>
  );
}

function emptyDeckTitle(inventoryEmpty: boolean, detachedOnly: boolean, query: string) {
  if (inventoryEmpty) return "No tmux sessions";
  if (detachedOnly && query.trim()) return "No matching detached sessions";
  if (detachedOnly) return "No detached sessions";
  return "No matching sessions";
}

function emptyDeckDetail(inventoryEmpty: boolean, detachedOnly: boolean, query: string) {
  if (inventoryEmpty) return "Create a session here or refresh after starting one elsewhere.";
  if (detachedOnly && query.trim()) return `No detached session name contains “${query.trim()}”.`;
  if (detachedOnly) return "Every tmux session currently has a terminal attached.";
  return `No session name contains “${query.trim()}”.`;
}

function recoveryStateMessage(state: string) {
  if (state === "blocked-live-sessions") return "Restore stopped because tmux sessions are already running.";
  if (state === "invalid-snapshot") return "The saved workspace failed validation and was not restored.";
  if (state === "invalid-request") return "The restore request failed validation.";
  return "The saved workspace could not be restored.";
}
