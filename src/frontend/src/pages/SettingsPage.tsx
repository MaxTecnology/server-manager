import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { AllowedProcess, SettingItem } from "../types";

type EditableSetting = {
  key: string;
  value: string;
  description: string;
};

type SettingsTab = "parameters" | "processes";

export function SettingsPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const [settings, setSettings] = useState<EditableSetting[]>([]);
  const [allowedProcesses, setAllowedProcesses] = useState<AllowedProcess[]>([]);
  const [newProcess, setNewProcess] = useState("");
  const [searchProcess, setSearchProcess] = useState("");
  const [activeTab, setActiveTab] = useState<SettingsTab>("parameters");
  const [loading, setLoading] = useState(false);

  const settingsMap = useMemo(() => {
    return new Map(settings.map((item) => [item.key, item]));
  }, [settings]);

  const filteredProcesses = useMemo(() => {
    const term = searchProcess.trim().toLowerCase();
    if (!term) {
      return allowedProcesses;
    }

    return allowedProcesses.filter((item) => {
      return (
        item.processName.toLowerCase().includes(term) ||
        item.createdBy.toLowerCase().includes(term)
      );
    });
  }, [allowedProcesses, searchProcess]);

  async function loadData() {
    setLoading(true);
    try {
      const [settingData, processData] = await Promise.all([
        apiRequest<SettingItem[]>("/api/settings", { token: auth.token }),
        apiRequest<AllowedProcess[]>("/api/allowed-processes", { token: auth.token })
      ]);

      setSettings(settingData.map((item) => ({ ...item })));
      setAllowedProcesses(processData);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar configurações.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  async function saveSetting(key: string) {
    const setting = settingsMap.get(key);
    if (!setting) {
      return;
    }

    try {
      await apiRequest(`/api/settings/${encodeURIComponent(key)}`, {
        method: "PUT",
        token: auth.token,
        body: {
          value: setting.value,
          description: setting.description
        }
      });
      pushToast("success", `Configuração ${key} salva.`);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao salvar configuração.");
    }
  }

  async function addProcess() {
    if (!newProcess.trim()) {
      return;
    }

    try {
      await apiRequest("/api/allowed-processes", {
        method: "POST",
        token: auth.token,
        body: { processName: newProcess }
      });
      setNewProcess("");
      pushToast("success", "Processo permitido cadastrado.");
      await loadData();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao cadastrar processo.");
    }
  }

  async function toggleProcess(item: AllowedProcess) {
    try {
      await apiRequest(`/api/allowed-processes/${item.id}/status`, {
        method: "PATCH",
        token: auth.token,
        body: { isActive: !item.isActive }
      });
      pushToast("success", `Processo ${item.processName} atualizado.`);
      await loadData();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao atualizar processo.");
    }
  }

  return (
    <section>
      <header className="page-header">
        <h2>Configurações</h2>
        <p>Parâmetros gerais e lista branca de processos permitidos.</p>
      </header>

      {loading && <p>Carregando...</p>}

      <div className="panel">
        <div className="ad-tabs" role="tablist" aria-label="Menu configurações">
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "parameters" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "parameters"}
            onClick={() => setActiveTab("parameters")}
          >
            Parâmetros
            <span className="status-pill status-unknown">{settings.length}</span>
          </button>
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "processes" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "processes"}
            onClick={() => setActiveTab("processes")}
          >
            Processos permitidos
            <span className="status-pill status-unknown">{allowedProcesses.length}</span>
          </button>
        </div>
      </div>

      {activeTab === "parameters" && (
        <div className="panel ad-tab-panel">
          <h3>Parâmetros do Sistema</h3>
          <div className="form-grid">
            {settings.map((setting) => (
              <div className="setting-row" key={setting.key}>
                <div>
                  <strong>{setting.key}</strong>
                  <p>{setting.description}</p>
                </div>
                <input
                  value={setting.value}
                  onChange={(event) =>
                    setSettings((current) =>
                      current.map((item) =>
                        item.key === setting.key ? { ...item, value: event.target.value } : item
                      )
                    )
                  }
                />
                <button className="secondary-button" type="button" onClick={() => void saveSetting(setting.key)}>
                  Salvar
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {activeTab === "processes" && (
        <div className="panel ad-tab-panel">
          <h3>Processos Permitidos</h3>
          <div className="toolbar">
            <input
              value={newProcess}
              onChange={(event) => setNewProcess(event.target.value)}
              placeholder="ex: excel.exe"
            />
            <button className="primary-button" type="button" onClick={() => void addProcess()}>
              Adicionar
            </button>
            <label>
              Buscar
              <input
                value={searchProcess}
                onChange={(event) => setSearchProcess(event.target.value)}
                placeholder="Processo ou criado por"
              />
            </label>
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Processo</th>
                  <th>Status</th>
                  <th>Criado por</th>
                  <th>Ação</th>
                </tr>
              </thead>
              <tbody>
                {filteredProcesses.map((item) => (
                  <tr key={item.id}>
                    <td>{item.processName}</td>
                    <td>{item.isActive ? "Ativo" : "Inativo"}</td>
                    <td>{item.createdBy}</td>
                    <td>
                      <button className="secondary-button" type="button" onClick={() => void toggleProcess(item)}>
                        {item.isActive ? "Desativar" : "Ativar"}
                      </button>
                    </td>
                  </tr>
                ))}
                {!filteredProcesses.length && (
                  <tr>
                    <td colSpan={4}>Nenhum processo encontrado.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
