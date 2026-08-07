import { lazy, Suspense, useEffect, useMemo, useRef, useState } from "react";
import { getClientConfig, login } from "./api";
import { SessionCard } from "./SessionCard";
import {
  orderSessionsByRecency,
  promoteSessionRecency,
  pruneSessionRecency,
  readSessionRecency,
  writeSessionRecency
} from "./sessionRecency";
import { useInventory } from "./useInventory";

const ACTIVE_KEY = "tmux-mobile-active-session";
const TerminalView = lazy(() => import("./TerminalView").then((module) => ({ default: module.TerminalView })));

export default function App() {
  const inventory = useInventory();
  const deck = useRef<HTMLDivElement>(null);
  const [activeId, setActiveId] = useState(() => localStorage.getItem(ACTIVE_KEY) ?? "");
  const [terminalId, setTerminalId] = useState<string | null>(null);
  const [apiKey, setApiKey] = useState("");
  const [loginError, setLoginError] = useState("");
  const [updateReady, setUpdateReady] = useState<ServiceWorker | null>(null);
  const [tmuxPrefix, setTmuxPrefix] = useState("C-b");
  const [recentSessionIds, setRecentSessionIds] = useState(() => readSessionRecency(localStorage));
  const orderedSessions = useMemo(
    () => orderSessionsByRecency(inventory.sessions, recentSessionIds),
    [inventory.sessions, recentSessionIds]
  );

  useEffect(() => {
    if (!orderedSessions.length) return;
    const valid = orderedSessions.some((session) => session.id === activeId);
    const next = valid ? activeId : orderedSessions[0].id;
    if (next !== activeId) setActiveId(next);
    requestAnimationFrame(() => {
      deck.current?.querySelector(`[data-session-id="${CSS.escape(next)}"]`)
        ?.scrollIntoView({ block: "start" });
    });
  }, [orderedSessions.map((session) => session.id).join("|")]);

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
    const session = orderedSessions[index];
    if (!session) return;
    deck.current?.querySelector(`[data-session-id="${CSS.escape(session.id)}"]`)
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const openTerminal = (sessionId: string) => {
    setRecentSessionIds((current) => {
      const next = promoteSessionRecency(current, sessionId);
      writeSessionRecency(localStorage, next);
      return next;
    });
    setActiveId(sessionId);
    setTerminalId(sessionId);
  };

  const activeIndex = Math.max(0, orderedSessions.findIndex((session) => session.id === activeId));
  const terminalSession = orderedSessions.find((session) => session.id === terminalId);
  if (terminalSession) return (
    <Suspense fallback={<State title="Opening terminal…" busy />}>
      <TerminalView session={terminalSession} tmuxPrefix={tmuxPrefix} onBack={() => setTerminalId(null)} />
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
        </form>
      </main>
    );
  }
  if (inventory.state === "loading") return <State title="Finding tmux sessions…" busy />;
  if (inventory.state === "error") return <State title="Unable to load sessions" detail={inventory.error}
    action={<button onClick={inventory.refresh}>Try again</button>} />;
  if (inventory.state === "empty") return <State title="No tmux sessions"
    detail="Start a tmux session on this host, then refresh."
    action={<button onClick={inventory.refresh}>Refresh</button>} />;

  return (
    <>
      {(!inventory.connected || updateReady) && <div className="alerts">
        {!inventory.connected && <div className="offline-banner" role="status">Offline — showing last live state</div>}
        {updateReady && (
          <div className="update-banner" role="status">
            <span>Update ready. Close active terminals before applying.</span>
            <button onClick={() => updateReady.postMessage({ type: "SKIP_WAITING" })}>Apply</button>
          </div>
        )}
      </div>}
      <main className={`session-deck ${(!inventory.connected || updateReady) ? "has-alerts" : ""}`}
        ref={deck} onScroll={() => {
        const cards = Array.from(deck.current?.querySelectorAll<HTMLElement>(".session-card") ?? []);
        const current = cards.reduce((best, card) =>
          Math.abs(card.getBoundingClientRect().top) < Math.abs(best.getBoundingClientRect().top) ? card : best,
          cards[0]);
        if (current?.dataset.sessionId) setActiveId(current.dataset.sessionId);
      }}>
        {orderedSessions.map((session, index) => (
          <SessionCard key={session.id} session={session} index={index} total={inventory.sessions.length}
            onTerminal={() => openTerminal(session.id)}
            onRefresh={inventory.refresh}
            onPrevious={() => scrollTo(index - 1)}
            onNext={() => scrollTo(index + 1)} />
        ))}
      </main>
      <div className="session-rail" aria-hidden="true">
        {orderedSessions.map((session, index) =>
          <span key={session.id} className={index === activeIndex ? "active" : ""} />)}
      </div>
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
