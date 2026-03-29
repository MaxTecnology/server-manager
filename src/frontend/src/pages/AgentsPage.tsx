import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { ServerItem } from "../types";

function formatDateTime(value: string | null) {
  if (!value) {
    return "Nunca";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString("pt-BR");
}

function formatRelative(value: string | null) {
  if (!value) {
    return "Nunca";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  const diffSeconds = Math.floor((Date.now() - date.getTime()) / 1000);
  if (diffSeconds < 10) {
    return "agora";
  }

  if (diffSeconds < 60) {
    return `${diffSeconds}s atrás`;
  }

  const diffMinutes = Math.floor(diffSeconds / 60);
  if (diffMinutes < 60) {
    return `${diffMinutes}m atrás`;
  }

  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) {
    return `${diffHours}h atrás`;
  }

  const diffDays = Math.floor(diffHours / 24);
  return `${diffDays}d atrás`;
}

export function AgentsPage() {
  const auth = useAuth();
  const { pushToast } = useToast();

  const [servers, setServers] = useState<ServerItem[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);

  async function load() {
    setLoading(true);
    try {
      const data = await apiRequest<ServerItem[]>("/api/servers", { token: auth.token });
      setServers(data);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar status dos agentes.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      void load();
    }, 30000);

    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) {
      return servers;
    }

    return servers.filter((item) => {
      return (
        item.name.toLowerCase().includes(term) ||
        item.hostname.toLowerCase().includes(term) ||
        (item.agentId ?? "").toLowerCase().includes(term)
      );
    });
  }, [search, servers]);

  function renderCapabilities(server: ServerItem) {
    const tags: string[] = [];
    if (server.supportsRds) {
      tags.push("RDS");
    }

    if (server.supportsAd) {
      tags.push("AD");
    }

    if (!tags.length) {
      tags.push("Nenhuma");
    }

    return (
      <div className="capability-list">
        {tags.map((tag) => (
          <span
            key={`${server.id}-${tag}`}
            className={tag === "Nenhuma" ? "capability-badge capability-none" : "capability-badge"}
          >
            {tag}
          </span>
        ))}
      </div>
    );
  }

  return (
    <section>
      <header className="page-header">
        <h2>Agentes</h2>
        <p>Status de conectividade do Agent Windows por servidor.</p>
      </header>

      <div className="panel">
        <div className="toolbar">
          <label>
            Buscar
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Servidor, hostname ou agentId"
            />
          </label>
          <button className="secondary-button" type="button" onClick={() => void load()}>
            Atualizar
          </button>
        </div>

        {loading ? (
          <p>Atualizando status dos agentes...</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Servidor</th>
                  <th>Hostname</th>
                  <th>Capacidades</th>
                  <th>AgentId</th>
                  <th>Status</th>
                  <th>Último heartbeat</th>
                  <th>Último snapshot</th>
                  <th>Ativo</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((server) => {
                  const statusClass = server.isAgentOnline
                    ? "status-pill status-online"
                    : server.agentLastHeartbeatUtc
                      ? "status-pill status-offline"
                      : "status-pill status-unknown";

                  const statusText = server.isAgentOnline
                    ? "Online"
                    : server.agentLastHeartbeatUtc
                      ? "Offline"
                      : "Sem agent";

                  return (
                    <tr key={server.id}>
                      <td>{server.name}</td>
                      <td>{server.hostname}</td>
                      <td>{renderCapabilities(server)}</td>
                      <td>{server.agentId ?? "-"}</td>
                      <td>
                        <span className={statusClass}>{statusText}</span>
                      </td>
                      <td title={formatDateTime(server.agentLastHeartbeatUtc)}>
                        {formatRelative(server.agentLastHeartbeatUtc)}
                      </td>
                      <td title={formatDateTime(server.agentSessionSnapshotUtc)}>
                        {server.hasRecentSnapshot
                          ? formatRelative(server.agentSessionSnapshotUtc)
                          : server.agentSessionSnapshotUtc
                            ? `desatualizado (${formatRelative(server.agentSessionSnapshotUtc)})`
                            : "Nunca"}
                      </td>
                      <td>{server.isActive ? "Sim" : "Não"}</td>
                    </tr>
                  );
                })}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={8}>Nenhum servidor encontrado.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  );
}
