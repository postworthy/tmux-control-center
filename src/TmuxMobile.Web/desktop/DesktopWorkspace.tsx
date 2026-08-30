import DesktopTerminal, { type TerminalConnectionState } from "./DesktopTerminal";
import type { WorkspaceDropZone, WorkspaceGroup, WorkspaceNode } from "./workspaceLayout";

export interface DesktopTab {
  sessionId: string;
  name: string;
}

interface DropTarget {
  groupId: string;
  zone: WorkspaceDropZone;
}

interface Props {
  layout: WorkspaceNode;
  tabs: DesktopTab[];
  connections: Record<string, TerminalConnectionState>;
  inventoryConnected: boolean;
  focusedGroupId: string;
  draggingSessionId: string | null;
  dropTarget: DropTarget | null;
  nativeProfilesAvailable: boolean;
  onFocusGroup: (group: WorkspaceGroup) => void;
  onActivate: (groupId: string, sessionId: string) => void;
  onClose: (sessionId: string) => void;
  onPopout: (sessionId: string) => void;
  onConnectionState: (sessionId: string, state: TerminalConnectionState) => void;
  onContextMenu: (sessionId: string, x: number, y: number) => void;
  onDragStart: (sessionId: string) => void;
  onDragEnd: () => void;
  onDragOver: (groupId: string, event: React.DragEvent<HTMLElement>) => void;
  onDragLeave: (groupId: string, event: React.DragEvent<HTMLElement>) => void;
  onDrop: (groupId: string, event: React.DragEvent<HTMLElement>) => void;
  onError: (message: string) => void;
}

export default function DesktopWorkspace(props: Props) {
  const renderGroup = (group: WorkspaceGroup) => {
    const activeTab = props.tabs.find(tab => tab.sessionId === group.activeId) ?? null;
    const target = props.dropTarget?.groupId === group.id ? props.dropTarget : null;
    return <section key={group.id}
      className={group.id === props.focusedGroupId ? "workspace-group focused" : "workspace-group"}
      onMouseDown={() => props.onFocusGroup(group)}
      onDragOver={event => props.onDragOver(group.id, event)}
      onDragLeave={event => props.onDragLeave(group.id, event)}
      onDrop={event => props.onDrop(group.id, event)}>
      <nav className="tab-strip group-tab-strip" aria-label="Session group tabs">
        {group.tabIds.map(sessionId => {
          const tab = props.tabs.find(item => item.sessionId === sessionId);
          if (!tab) return null;
          return <div draggable
            className={tab.sessionId === group.activeId ? "tab active" : "tab"} key={tab.sessionId}
            onDragStart={event => {
              event.dataTransfer.effectAllowed = "move";
              event.dataTransfer.setData("text/plain", tab.sessionId);
              props.onDragStart(tab.sessionId);
            }} onDragEnd={props.onDragEnd}>
            <button onClick={() => props.onActivate(group.id, tab.sessionId)}>{tab.name}</button>
            {props.nativeProfilesAvailable && <button className="tab-popout"
              title={`Open ${tab.name} in a new window`} aria-label={`Open ${tab.name} in a new window`}
              onClick={() => props.onPopout(tab.sessionId)}>↗</button>}
            <button className="tab-close" aria-label={`Detach ${tab.name}`}
              onClick={() => props.onClose(tab.sessionId)}>×</button>
          </div>;
        })}
        <span className={`connection ${activeTab ? props.connections[activeTab.sessionId] ?? "connecting" : ""}`}>
          {!props.inventoryConnected ? "server offline" : activeTab
            ? props.connections[activeTab.sessionId] ?? "connecting"
            : "empty group"}
        </span>
      </nav>
      <div className="terminal-stage">
        {group.tabIds.map(sessionId => <DesktopTerminal key={sessionId} sessionId={sessionId}
          active={sessionId === group.activeId}
          onConnectionState={state => props.onConnectionState(sessionId, state)}
          onContextMenu={(x, y) => props.onContextMenu(sessionId, x, y)}
          onError={props.onError} />)}
        {!activeTab && <div className="empty-state"><h1>Select a session</h1><p>Open a session or drag a tab into this group.</p></div>}
      </div>
      {props.draggingSessionId && <div className="drop-guidance" aria-hidden="true">
        {(["left", "right", "top", "bottom", "center"] as WorkspaceDropZone[]).map(zone =>
          <span key={zone} className={`drop-zone ${zone}${target?.zone === zone ? " active" : ""}`} />)}
      </div>}
    </section>;
  };

  const renderNode = (node: WorkspaceNode): React.ReactNode => node.kind === "group"
    ? renderGroup(node)
    : <div key={node.id} className={`workspace-split ${node.direction}`}>
        {renderNode(node.first)}
        <div className="split-divider" aria-hidden="true" />
        {renderNode(node.second)}
      </div>;

  return <div className="workspace-layout">{renderNode(props.layout)}</div>;
}
