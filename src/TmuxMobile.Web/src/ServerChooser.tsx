import { useMemo, useState } from "react";
import {
  clearServerProfiles,
  deleteServerProfile,
  loadServerProfiles,
  MAX_SERVER_PROFILES,
  normalizeServerOrigin,
  saveServerProfiles,
  type ServerProfile,
  upsertServerProfile
} from "./serverProfiles";

interface ServerChooserProps {
  currentOrigin: string;
  launcherOrigin: string | null;
  onClose: () => void;
  onOpenServer: (serverUrl: string) => void;
  onReturnToLauncher: () => void;
}

export function ServerChooser({
  currentOrigin,
  launcherOrigin,
  onClose,
  onOpenServer,
  onReturnToLauncher
}: ServerChooserProps) {
  const initial = useMemo(() => loadServerProfiles(localStorage), []);
  const [profiles, setProfiles] = useState(initial.profiles);
  const [storageError, setStorageError] = useState(initial.error);
  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [label, setLabel] = useState("");
  const [serverUrl, setServerUrl] = useState("");
  const [formError, setFormError] = useState("");

  const startAdd = () => {
    setEditingId(null);
    setLabel("");
    setServerUrl("");
    setFormError("");
    setFormOpen(true);
  };

  const startEdit = (profile: ServerProfile) => {
    setEditingId(profile.id);
    setLabel(profile.label);
    setServerUrl(profile.serverUrl);
    setFormError("");
    setFormOpen(true);
  };

  const persist = (next: ServerProfile[]) => {
    const saved = saveServerProfiles(localStorage, next);
    setProfiles(saved);
    setStorageError(null);
  };

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    setFormError("");
    try {
      if (storageError) throw new Error("Clear the invalid saved data before adding or editing servers.");
      const normalized = normalizeServerOrigin(serverUrl);
      if (normalized === currentOrigin) throw new Error("The current server is already available above.");
      persist(upsertServerProfile(profiles, { id: editingId ?? undefined, label, serverUrl: normalized }));
      setFormOpen(false);
    } catch (reason) {
      setFormError(reason instanceof Error ? reason.message : "Unable to save this server.");
    }
  };

  const remove = (profile: ServerProfile) => {
    try {
      persist(deleteServerProfile(profiles, profile.id));
      if (editingId === profile.id) setFormOpen(false);
    } catch (reason) {
      setStorageError(reason instanceof Error ? reason.message : "Unable to delete this server.");
    }
  };

  const clearInvalidData = () => {
    try {
      clearServerProfiles(localStorage);
      setProfiles([]);
      setStorageError(null);
      setFormOpen(false);
    } catch (reason) {
      setStorageError(reason instanceof Error ? reason.message : "Unable to clear saved servers.");
    }
  };

  return (
    <main className="server-chooser">
      <header className="server-chooser-header">
        <div>
          <span className="server-eyebrow">tmuxctl</span>
          <h1>Servers</h1>
        </div>
        <button type="button" onClick={onClose} aria-label="Close servers">Close</button>
      </header>

      <section className="server-list" aria-label="Available servers">
        {launcherOrigin && (
          <article className="server-row launcher-row">
            <div>
              <strong>Launcher</strong>
              <small>{displayOrigin(launcherOrigin)}</small>
            </div>
            <button type="button" onClick={onReturnToLauncher}>Return</button>
          </article>
        )}
        <article className="server-row current-server-row">
          <div>
            <strong>This server</strong>
            <small>{displayOrigin(currentOrigin)}</small>
          </div>
          <span className="current-server-badge">Current</span>
        </article>

        {profiles.map((profile) => (
          <article className="server-row" key={profile.id}>
            <button className="server-open-button" type="button" onClick={() => onOpenServer(profile.serverUrl)}>
              <strong>{profile.label}</strong>
              <small>{displayOrigin(profile.serverUrl)}</small>
            </button>
            <div className="server-row-actions">
              <button type="button" onClick={() => startEdit(profile)} aria-label={`Edit ${profile.label}`}>Edit</button>
              <button className="server-delete-button" type="button" onClick={() => remove(profile)}
                aria-label={`Delete ${profile.label}`}>Delete</button>
            </div>
          </article>
        ))}
      </section>

      {storageError && (
        <section className="server-storage-error" role="alert">
          <p>{storageError}</p>
          <button type="button" onClick={clearInvalidData}>Clear saved profiles</button>
        </section>
      )}

      {!formOpen && (
        <button className="server-add-button" type="button" onClick={startAdd}
          disabled={Boolean(storageError) || profiles.length >= MAX_SERVER_PROFILES}>
          + Add server
        </button>
      )}

      {formOpen && (
        <form className="server-form" onSubmit={submit}>
          <h2>{editingId ? "Edit server" : "Add server"}</h2>
          <label htmlFor="server-label">Label</label>
          <input id="server-label" value={label} required maxLength={80} autoFocus
            autoComplete="off" onChange={(event) => setLabel(event.target.value)} />
          <label htmlFor="server-url">HTTPS server URL</label>
          <input id="server-url" value={serverUrl} required type="url" inputMode="url"
            placeholder="https://host.example.ts.net:8443" autoComplete="url" autoCapitalize="none"
            spellCheck={false} onChange={(event) => setServerUrl(event.target.value)} />
          {formError && <p className="error-text" role="alert">{formError}</p>}
          <div className="server-form-actions">
            <button type="button" onClick={() => setFormOpen(false)}>Cancel</button>
            <button className="terminal-button" type="submit">Save server</button>
          </div>
        </form>
      )}

      <p className="server-privacy-note">
        Labels and server addresses stay on this device. Opening a server navigates this app to that origin.
      </p>
    </main>
  );
}

function displayOrigin(origin: string): string {
  try {
    return new URL(origin).host;
  } catch {
    return origin;
  }
}
