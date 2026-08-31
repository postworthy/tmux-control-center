import DesktopTerminal, { type TerminalConnectionState } from "./DesktopTerminal";
import {
  WORKSPACE_DROP_ZONES,
  type WorkspaceDropZone,
  type WorkspaceGroup,
  type WorkspaceNode
} from "./workspaceLayout";

export interface DesktopTab {
  sessionId: string;
  name: string;
}

interface Props {
  layout: WorkspaceNode;
  tabs: DesktopTab[];
  connections: Record<string, TerminalConnectionState>;
  inventoryConnected: boolean;
  focusedGroupId: string;
  draggingSessionId: string | null;
  dropTarget: WorkspaceDropZone | null;
  nativeProfilesAvailable: boolean;
  applicationScrollSessionIds: ReadonlySet<string>;
  onFocusGroup: (group: WorkspaceGroup) => void;
  onActivate: (groupId: string, sessionId: string) => void;
  onClose: (sessionId: string) => void;
  onPopout: (sessionId: string) => void;
  onConnectionState: (sessionId: string, state: TerminalConnectionState) => void;
  onDragStart: (sessionId: string) => void;
  onDragEnd: () => void;
  onDragOver: (event: React.DragEvent<HTMLElement>) => void;
  onDragLeave: (event: React.DragEvent<HTMLElement>) => void;
  onDrop: (event: React.DragEvent<HTMLElement>) => void;
  onError: (message: string) => void;
}

export default function DesktopWorkspace(props: Props) {
  const dropLabels: Record<WorkspaceDropZone, string> = {
    left: "Split left",
    right: "Split right",
    top: "Split top",
    bottom: "Split bottom",
    center: "Single view"
  };
  const renderGroup = (group: WorkspaceGroup) => {
    const activeTab = props.tabs.find(tab => tab.sessionId === group.activeId) ?? null;
    return <section key={group.id}
      className={group.id === props.focusedGroupId ? "workspace-group focused" : "workspace-group"}
      onMouseDown={() => props.onFocusGroup(group)}>
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
          applicationScrollEnabled={props.applicationScrollSessionIds.has(sessionId)}
          onConnectionState={state => props.onConnectionState(sessionId, state)}
          onError={props.onError} />)}
        {!activeTab && <div className="empty-state"><h1>Select a session</h1><p>Open a session or drag a tab into this group.</p></div>}
      </div>
    </section>;
  };

  const renderNode = (node: WorkspaceNode): React.ReactNode => node.kind === "group"
    ? renderGroup(node)
    : <div key={node.id} className={`workspace-split ${node.direction}`}>
        {renderNode(node.first)}
        <div className="split-divider" aria-hidden="true" />
        {renderNode(node.second)}
      </div>;

  return <div className="workspace-layout" onDragOver={props.onDragOver}
    onDragLeave={props.onDragLeave} onDrop={props.onDrop}>
    {renderNode(props.layout)}
    {props.draggingSessionId && <div className="drop-guidance" aria-hidden="true">
      {WORKSPACE_DROP_ZONES.map(zone => <span key={zone}
        className={`drop-zone ${zone}${props.dropTarget === zone ? " active" : ""}`}>
        <span>{dropLabels[zone]}</span>
      </span>)}
    </div>}
  </div>;
}
