import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "../api/client";
import { Modal } from "../components/Modal";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { ServerItem, SessionInfo } from "../types";

type PendingAction =
  | { type: "disconnect"; session: SessionInfo }
  | { type: "logoff"; session: SessionInfo };

export function SessionsPage() {
  const auth = useAuth();
  const { pushToast } = useToast();

  const [servers, setServers] = useState<ServerItem[]>([]);
  const [selectedServer, setSelectedServer] = useState<string>("");
  const [sessions, setSessions] = useState<SessionInfo[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null);
  const [processSession, setProcessSession] = useState<SessionInfo | null>(null);
  const [processName, setProcessName] = useState("");

  async function loadServers() {
    try {
      const data = await apiRequest<ServerItem[]>("/api/servers", { token: auth.token });
      setServers(data);
      const defaultServer = data.find((item) => item.isDefault);
      setSelectedServer(defaultServer?.hostname ?? data[0]?.hostname ?? "");
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar servidores.");
    }
  }

  async function loadSessions(serverName = selectedServer) {
    if (!serverName) {
      return;
    }

    setLoading(true);
    try {
      const data = await apiRequest<SessionInfo[]>(
        `/api/sessions?serverName=${encodeURIComponent(serverName)}`,
        { token: auth.token }
      );
      setSessions(data);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar sessões.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadServers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  useEffect(() => {
    if (!selectedServer) {
      return;
    }

    void loadSessions(selectedServer);
    const timer = window.setInterval(() => {
      void loadSessions(selectedServer);
    }, 30000);

    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedServer]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return sessions;
    }

    return sessions.filter((item) => {
      return (
        item.username.toLowerCase().includes(term) ||
        item.state.toLowerCase().includes(term) ||
        item.sessionId.toString().includes(term)
      );
    });
  }, [search, sessions]);

  async function executeDisconnect(session: SessionInfo) {
    await apiRequest("/api/sessions/disconnect", {
      method: "POST",
      token: auth.token,
      body: {
        sessionId: session.sessionId,
        serverName: session.serverName,
        targetUsername: session.username
      }
    });
    pushToast("success", `Sessão ${session.sessionId} desconectada.`);
    await loadSessions();
  }

  async function executeLogoff(session: SessionInfo) {
    await apiRequest("/api/sessions/logoff", {
      method: "POST",
      token: auth.token,
      body: {
        sessionId: session.sessionId,
        serverName: session.serverName,
        targetUsername: session.username
      }
    });
    pushToast("success", `Logoff da sessão ${session.sessionId} concluído.`);
    await loadSessions();
  }

  async function executeKillProcess() {
    if (!processSession || !processName.trim()) {
      return;
    }

    try {
      await apiRequest("/api/sessions/kill-process", {
        method: "POST",
        token: auth.token,
        body: {
          sessionId: processSession.sessionId,
          serverName: processSession.serverName,
          targetUsername: processSession.username,
          processName
        }
      });

      pushToast("success", `Processo ${processName} encerrado na sessão ${processSession.sessionId}.`);
      setProcessSession(null);
      setProcessName("");
      await loadSessions();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao encerrar processo.");
    }
  }

  return (
    <section>
      <header className="page-header">
        <h2>Sessões</h2>
        <p>Listagem em tempo real com ações administrativas controladas.</p>
      </header>

      <div className="panel">
        <div className="toolbar">
          <label>
            Servidor
            <select value={selectedServer} onChange={(event) => setSelectedServer(event.target.value)}>
              {servers.map((server) => (
                <option value={server.hostname} key={server.id}>
                  {server.name} ({server.hostname})
                </option>
              ))}
            </select>
          </label>
          <label>
            Buscar
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Usuário, ID ou estado"
            />
          </label>
          <button className="secondary-button" type="button" onClick={() => void loadSessions()}>
            Atualizar
          </button>
        </div>

        {loading ? (
          <p>Atualizando sessões...</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Usuário</th>
                  <th>Sessão</th>
                  <th>Estado</th>
                  <th>Ocioso</th>
                  <th>Logon</th>
                  <th>Servidor</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((session) => (
                  <tr key={`${session.serverName}-${session.sessionId}`}>
                    <td>{session.sessionId}</td>
                    <td>{session.username || "-"}</td>
                    <td>{session.sessionName || "-"}</td>
                    <td>{session.state}</td>
                    <td>{session.idleTime}</td>
                    <td>{session.logonTime}</td>
                    <td>{session.serverName}</td>
                    <td>
                      <div className="button-row">
                        <button
                          className="secondary-button"
                          type="button"
                          onClick={() => setPendingAction({ type: "disconnect", session })}
                        >
                          Desconectar
                        </button>
                        {auth.isAdministrator && (
                          <button
                            className="danger-button"
                            type="button"
                            onClick={() => setPendingAction({ type: "logoff", session })}
                          >
                            Logoff
                          </button>
                        )}
                        <button
                          className="secondary-button"
                          type="button"
                          onClick={() => {
                            setProcessSession(session);
                            setProcessName("");
                          }}
                        >
                          Encerrar processo
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={8}>Nenhuma sessão encontrada.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {pendingAction && (
        <Modal
          title={pendingAction.type === "disconnect" ? "Confirmar desconexão" : "Confirmar logoff"}
          onClose={() => setPendingAction(null)}
        >
          <p>
            Confirma a ação na sessão <strong>{pendingAction.session.sessionId}</strong> do usuário{" "}
            <strong>{pendingAction.session.username || "N/A"}</strong>?
          </p>
          <div className="button-row">
            <button className="secondary-button" type="button" onClick={() => setPendingAction(null)}>
              Cancelar
            </button>
            <button
              className={pendingAction.type === "disconnect" ? "primary-button" : "danger-button"}
              type="button"
              onClick={async () => {
                try {
                  if (pendingAction.type === "disconnect") {
                    await executeDisconnect(pendingAction.session);
                  } else {
                    await executeLogoff(pendingAction.session);
                  }
                } catch (error) {
                  pushToast("error", error instanceof Error ? error.message : "Falha na operação.");
                } finally {
                  setPendingAction(null);
                }
              }}
            >
              Confirmar
            </button>
          </div>
        </Modal>
      )}

      {processSession && (
        <Modal title="Encerrar processo na sessão" onClose={() => setProcessSession(null)}>
          <div className="form-grid">
            <label>
              Processo (ex: excel.exe)
              <input value={processName} onChange={(event) => setProcessName(event.target.value)} />
            </label>
            <div className="button-row">
              <button className="secondary-button" type="button" onClick={() => setProcessSession(null)}>
                Cancelar
              </button>
              <button className="danger-button" type="button" onClick={() => void executeKillProcess()}>
                Encerrar
              </button>
            </div>
          </div>
        </Modal>
      )}
    </section>
  );
}
