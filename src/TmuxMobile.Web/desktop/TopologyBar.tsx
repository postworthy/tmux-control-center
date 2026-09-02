import type { TmuxTopology } from "./desktopApi";

interface Props {
  topology: TmuxTopology | null;
  busy: boolean;
  onCreateWindow: () => void;
  onSelectWindow: (id: string) => void;
  onCloseWindow: (id: string, name: string) => void;
  onSelectPane: (id: string) => void;
  onSplitPane: (id: string, orientation: "horizontal" | "vertical") => void;
  onResizePane: (id: string, direction: "left" | "right" | "up" | "down") => void;
  onClosePane: (id: string, index: number) => void;
}

export default function TopologyBar({ topology, busy, onCreateWindow, onSelectWindow,
  onCloseWindow, onSelectPane, onSplitPane, onResizePane, onClosePane }: Props) {
  const activeWindow = topology?.windows.find(window => window.isActive) ?? topology?.windows[0];
  const activePane = activeWindow?.panes.find(pane => pane.isActive) ?? activeWindow?.panes[0];

  return <section className="topology-bar" aria-label="Tmux windows and panes">
    <div className="window-strip">
      {topology?.windows.map(window => <div className={window.id === activeWindow?.id ? "window-tab active" : "window-tab"}
        key={window.id}>
        <button disabled={busy} onClick={() => onSelectWindow(window.id)}>{window.index}:{window.name}</button>
        <button className="topology-close" disabled={busy} aria-label={`Close window ${window.name}`}
          onClick={() => onCloseWindow(window.id, window.name)}>×</button>
      </div>)}
      <button className="topology-add" disabled={busy || !topology} title="Create tmux window"
        onClick={onCreateWindow}>+</button>
    </div>
    <div className="pane-strip">
      <span className="topology-label">Panes</span>
      {activeWindow?.panes.map(pane => <button key={pane.id}
        className={pane.id === activePane?.id ? "pane-chip active" : "pane-chip"}
        disabled={busy} onClick={() => onSelectPane(pane.id)}
        title={`${pane.currentCommand} — ${pane.currentWorkingDirectory}`}>
        {pane.paneIndex} · {pane.currentCommand} <small>{pane.width}×{pane.height}</small>
      </button>)}
      {activePane && <div className="pane-actions">
        <button disabled={busy} title="Split left/right" onClick={() => onSplitPane(activePane.id, "horizontal")}>⇆</button>
        <button disabled={busy} title="Split top/bottom" onClick={() => onSplitPane(activePane.id, "vertical")}>⇅</button>
        <button disabled={busy} title="Resize left" onClick={() => onResizePane(activePane.id, "left")}>←</button>
        <button disabled={busy} title="Resize right" onClick={() => onResizePane(activePane.id, "right")}>→</button>
        <button disabled={busy} title="Resize up" onClick={() => onResizePane(activePane.id, "up")}>↑</button>
        <button disabled={busy} title="Resize down" onClick={() => onResizePane(activePane.id, "down")}>↓</button>
        <button className="topology-close" disabled={busy} title="Close pane"
          onClick={() => onClosePane(activePane.id, activePane.paneIndex)}>×</button>
      </div>}
    </div>
  </section>;
}
