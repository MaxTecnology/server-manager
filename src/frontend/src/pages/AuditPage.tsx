import { useEffect, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { AuditLog, PagedResult } from "../types";

export function AuditPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<AuditLog> | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const response = await apiRequest<PagedResult<AuditLog>>(
          `/api/audit?page=${page}&pageSize=20&search=${encodeURIComponent(search)}`,
          { token: auth.token }
        );
        setData(response);
      } catch (error) {
        pushToast("error", error instanceof Error ? error.message : "Falha ao carregar auditoria.");
      } finally {
        setLoading(false);
      }
    }

    void load();
  }, [auth.token, page, pushToast, search]);

  return (
    <section>
      <header className="page-header">
        <h2>Auditoria</h2>
        <p>Histórico de ações administrativas realizadas no sistema.</p>
      </header>

      <div className="panel">
        <div className="toolbar">
          <label>
            Buscar
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
              placeholder="Operador, usuário, processo ou ação"
            />
          </label>
        </div>

        {loading ? (
          <p>Carregando auditoria...</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Data/Hora (UTC)</th>
                  <th>Operador</th>
                  <th>Ação</th>
                  <th>Servidor</th>
                  <th>Sessão</th>
                  <th>Usuário Alvo</th>
                  <th>Processo</th>
                  <th>Resultado</th>
                  <th>Erro</th>
                </tr>
              </thead>
              <tbody>
                {data?.items.map((item) => (
                  <tr key={item.id}>
                    <td>{new Date(item.timestampUtc).toLocaleString("pt-BR")}</td>
                    <td>{item.operatorUsername}</td>
                    <td>{item.action}</td>
                    <td>{item.serverName}</td>
                    <td>{item.sessionId ?? "-"}</td>
                    <td>{item.targetUsername ?? "-"}</td>
                    <td>{item.processName ?? "-"}</td>
                    <td>{item.success ? "Sucesso" : "Erro"}</td>
                    <td>{item.errorMessage ?? "-"}</td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={9}>Nenhum registro encontrado.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {data && (
          <div className="pagination">
            <button className="secondary-button" type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Anterior
            </button>
            <span>
              Página {data.page} de {Math.max(1, Math.ceil(data.totalCount / data.pageSize))}
            </span>
            <button
              className="secondary-button"
              type="button"
              disabled={data.page * data.pageSize >= data.totalCount}
              onClick={() => setPage((p) => p + 1)}
            >
              Próxima
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
