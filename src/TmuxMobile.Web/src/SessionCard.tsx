import { useState } from "react";
import { action, killSession } from "./api";
import type { TmuxSession } from "./types";

interface Props {
  session: TmuxSession;
  index: number;
  total: number;
  onTerminal: () => void;
  onRefresh: () => void | Promise<void>;
  onPrevious: () => void;
  onNext: () => void;
}

export function SessionCard({
  session, index, total, onTerminal, onRefresh, onPrevious, onNext
}: Props) {
  const [details, setDetails] = useState(false);
  const [actions, setActions] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [killConfirm, setKillConfirm] = useState(false);
  const [killBusy, setKillBusy] = useState(false);
  const [killError, setKillError] = useState("");

  const run = async (operation: () => Promise<void>) => {
    setBusy(true);
    setActionError("");
    try { await operation(); onRefresh(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Action failed"); }
    finally { setBusy(false); setActions(false); }
  };
  const rename = async () => {
    const name = window.prompt("New session name", session.name);
    if (name) await run(() => action(`/api/sessions/${session.id}/rename`, { name }));
  };
  const confirmKill = async () => {
    setKillBusy(true);
    setKillError("");
    try {
      await killSession(session.id);
      setKillConfirm(false);
      setKillBusy(false);
      void onRefresh();
    } catch (reason) {
      setKillError(reason instanceof Error ? reason.message : "Unable to kill session");
      setKillBusy(false);
    }
  };

  return (
    <article className={`session-card${session.isAttached ? "" : " session-card-detached"}`}
      data-session-id={session.id} aria-labelledby={`title-${session.id}`}>
      <div className="card-shell">
        <header className="session-header">
          <button className="session-title" onClick={() => setDetails(true)}
            aria-label={`Show details for ${session.name}`}>
            <span className={`status-dot status-${session.status}`} aria-hidden="true" />
            <span>
              <small>Session {index + 1} of {total}</small>
              <h1 id={`title-${session.id}`}>{session.name}</h1>
            </span>
          </button>
          <button className="icon-button" onClick={() => setActions(!actions)}
            aria-label={`Quick actions for ${session.name}`} aria-expanded={actions}>•••</button>
        </header>

        <div className="status-row">
          <span className={`status-pill status-${session.status}`}>{session.status}</span>
          <span>{session.isAttached ? `${session.attachedClientCount} attached` : "detached"}</span>
          <span>{relativeTime(session.lastActivityAt)}</span>
        </div>

        {!session.isAttached && (
          <div className="detached-notice" role="status">
            <span className="detached-notice-icon" aria-hidden="true">○</span>
            <span>
              <strong>No terminal attached</strong>
              <small>The tmux session is still running.</small>
            </span>
          </div>
        )}

        <dl className="metadata">
          <div><dt>Running</dt><dd>{session.currentCommand || "Unknown"}</dd></div>
          <div><dt>Path</dt><dd title={session.currentWorkingDirectory}>{shortPath(session.currentWorkingDirectory)}</dd></div>
          {session.title && <div><dt>Pane</dt><dd>{session.title}</dd></div>}
          <div><dt>Layout</dt><dd>{session.windowCount} window{session.windowCount === 1 ? "" : "s"} · {session.paneCount} pane{session.paneCount === 1 ? "" : "s"}</dd></div>
        </dl>

        <section className="preview" aria-label="Recent terminal output">
          <pre>{session.previewText || "No recent output."}</pre>
          <div className="preview-fade" aria-hidden="true" />
          {session.previewTruncated && <span className="truncated">Preview truncated</span>}
        </section>

        <div className="primary-actions">
          <button className="terminal-button" onClick={onTerminal}>Terminal</button>
          <button onClick={() => { void run(() => action(`/api/panes/${session.currentPaneId}/keys`, { keys: ["enter"] })); }}
            disabled={busy}>Send Enter</button>
        </div>
        {actionError && <p className="card-error" role="alert">{actionError}</p>}

        <nav className="deck-controls" aria-label="Session navigation">
          <button onClick={onPrevious} disabled={index === 0} aria-label="Previous session">↑</button>
          <span>{index + 1} / {total}</span>
          <button onClick={onNext} disabled={index === total - 1} aria-label="Next session">↓</button>
        </nav>

        {actions && (
          <div className="action-menu" role="menu">
            <button role="menuitem" onClick={onTerminal}>Open terminal</button>
            <button role="menuitem" onClick={() => { void run(() => action(`/api/panes/${session.currentPaneId}/interrupt`)); }}>Send Ctrl-C</button>
            <button role="menuitem" onClick={onRefresh}>Refresh output</button>
            <button role="menuitem" onClick={() => navigator.clipboard.writeText(session.previewText)}>Copy recent output</button>
            <button role="menuitem" onClick={() => void rename()}>Rename session</button>
            <button className="danger-menu-item" role="menuitem" onClick={() => {
              setActions(false);
              setKillError("");
              setKillConfirm(true);
            }}>Kill session</button>
          </div>
        )}
      </div>

      {details && (
        <div className="modal-backdrop" role="presentation" onClick={() => setDetails(false)}>
          <section className="details-dialog" role="dialog" aria-modal="true"
            aria-labelledby="details-title" onClick={(event) => event.stopPropagation()}>
            <h2 id="details-title">{session.name}</h2>
            <p>{session.statusReason}</p>
            <dl className="details-list">
              <dt>Created</dt><dd>{new Date(session.createdAt).toLocaleString()}</dd>
              <dt>Last activity</dt><dd>{new Date(session.lastActivityAt).toLocaleString()}</dd>
              <dt>Window</dt><dd>{session.currentWindowName || "Unknown"}</dd>
              <dt>Working directory</dt><dd>{session.currentWorkingDirectory || "Unknown"}</dd>
            </dl>
            <button onClick={() => setDetails(false)}>Close</button>
          </section>
        </div>
      )}

      {killConfirm && (
        <div className="modal-backdrop" role="presentation"
          onClick={() => !killBusy && setKillConfirm(false)}>
          <section className="kill-dialog" role="dialog" aria-modal="true"
            aria-labelledby={`kill-title-${session.id}`} onClick={(event) => event.stopPropagation()}>
            <h2 id={`kill-title-${session.id}`}>Kill “{session.name}”?</h2>
            <p>This permanently ends the tmux session and programs running inside it. This cannot be undone.</p>
            {killError && <p className="error-text" role="alert">{killError}</p>}
            <div className="kill-actions">
              <button autoFocus onClick={() => setKillConfirm(false)} disabled={killBusy}>Cancel</button>
              <button className="danger-button" onClick={() => void confirmKill()} disabled={killBusy}>
                {killBusy ? "Killing…" : "Kill session"}
              </button>
            </div>
          </section>
        </div>
      )}
    </article>
  );
}

function shortPath(path: string) {
  if (path.length <= 44) return path || "Unknown";
  return `…${path.slice(-43)}`;
}

function relativeTime(value: string) {
  const seconds = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 1000));
  if (seconds < 60) return "just now";
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
  return `${Math.floor(seconds / 86400)}d ago`;
}
