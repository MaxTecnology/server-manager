import { useEffect, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { DashboardMetrics } from "../types";

export function DashboardPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const data = await apiRequest<DashboardMetrics>("/api/dashboard/metrics", {
          token: auth.token
        });
        setMetrics(data);
      } catch (error) {
        pushToast("error", error instanceof Error ? error.message : "Falha ao carregar dashboard.");
      } finally {
        setLoading(false);
      }
    }

    void load();
  }, [auth.token, pushToast]);

  return (
    <section>
      <header className="page-header">
        <h2>Dashboard</h2>
        <p>Resumo operacional do ambiente de sessões.</p>
      </header>

      {loading && <p>Carregando métricas...</p>}

      {metrics && (
        <div className="metrics-grid">
          <article className="metric-card">
            <p>Sessões Ativas</p>
            <strong>{metrics.activeSessions}</strong>
          </article>
          <article className="metric-card">
            <p>Sessões Desconectadas</p>
            <strong>{metrics.disconnectedSessions}</strong>
          </article>
          <article className="metric-card">
            <p>Ações Hoje</p>
            <strong>{metrics.actionsToday}</strong>
          </article>
          <article className="metric-card metric-card-error">
            <p>Erros Hoje</p>
            <strong>{metrics.errorsToday}</strong>
          </article>
        </div>
      )}
    </section>
  );
}
