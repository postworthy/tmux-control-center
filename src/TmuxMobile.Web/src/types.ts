export type SessionStatus =
  "active" | "idle" | "attached" | "detached" | "waiting" | "completed" | "failed" | "unknown";

export interface TmuxSession {
  id: string;
  name: string;
  createdAt: string;
  lastActivityAt: string;
  isAttached: boolean;
  attachedClientCount: number;
  windowCount: number;
  paneCount: number;
  currentWindowName: string;
  currentPaneId: string;
  currentCommand: string;
  currentWorkingDirectory: string;
  title: string;
  status: SessionStatus;
  statusReason: string;
  previewText: string;
  previewTruncated: boolean;
}

export interface InventorySnapshot {
  version: number;
  updatedAt: string;
  sessions: TmuxSession[];
}

export interface WorkspaceRecoveryStatus {
  enabled: boolean;
  snapshotAvailable: boolean;
  snapshotSavedAt: string | null;
  requestPending: boolean;
  state: string;
  updatedAt: string | null;
  restoredSessionCount: number;
  requestId: string | null;
}
