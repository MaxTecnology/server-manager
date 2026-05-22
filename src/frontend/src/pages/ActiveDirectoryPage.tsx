import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type {
  AdOrganizationalUnit,
  AdUserSearchItem,
  AgentCommand,
  CreateAdUserRequest,
  ResetAdUserPasswordRequest,
  SearchAdUsersRequest,
  ServerItem
} from "../types";

function formatDateTime(value: string | null) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString("pt-BR");
}

function getStatusClass(status: string) {
  const normalized = status.trim().toLowerCase();
  if (normalized === "succeeded") {
    return "status-pill status-online";
  }

  if (normalized === "failed") {
    return "status-pill status-offline";
  }

  if (normalized === "running") {
    return "status-pill status-unknown";
  }

  return "status-pill status-unknown";
}

function formatOuOption(item: AdOrganizationalUnit) {
  const depth = Math.max(0, item.depth);
  const prefix = depth === 0 ? "" : `${"--".repeat(Math.min(depth, 8))} `;
  return `${prefix}${item.name} (${item.canonicalName})`;
}

function getAdUserStatusLabel(user: AdUserSearchItem) {
  if (!user.enabled) {
    return "Bloqueado";
  }

  if (user.lockedOut) {
    return "Travado";
  }

  return "Ativo";
}

function getAdUserStatusClass(user: AdUserSearchItem) {
  if (!user.enabled) {
    return "status-pill status-offline";
  }

  if (user.lockedOut) {
    return "status-pill status-unknown";
  }

  return "status-pill status-online";
}

function getCommandStatusText(status: string, detailed = false) {
  const normalized = status.trim().toLowerCase();
  if (normalized === "succeeded") {
    return detailed ? "Concluído com sucesso" : "Concluído";
  }

  if (normalized === "failed") {
    return detailed ? "Falha na execução" : "Falhou";
  }

  if (normalized === "running") {
    return detailed ? "Comando em execução" : "Em execução";
  }

  if (normalized === "pending") {
    return detailed ? "Comando na fila de execução" : "Na fila";
  }

  return status;
}

function getCommandResultDescription(resultOutput: string | null) {
  const raw = resultOutput?.trim();
  if (!raw) {
    return null;
  }

  const knownResults: Record<string, string> = {
    AD_USER_CREATE_OK: "Usuário criado no Active Directory com sucesso.",
    AD_PASSWORD_RESET_OK: "Senha redefinida no Active Directory com sucesso.",
    AD_USER_BLOCK_OK: "Usuário bloqueado no Active Directory com sucesso.",
    AD_USER_UNBLOCK_OK: "Usuário desbloqueado no Active Directory com sucesso."
  };

  return knownResults[raw] ?? null;
}

type ActiveDirectoryTab = "users" | "create" | "reset" | "commands";

export function ActiveDirectoryPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const finalStatusToastRef = useRef<string | null>(null);

  const [servers, setServers] = useState<ServerItem[]>([]);
  const [selectedServerId, setSelectedServerId] = useState<string>("");
  const [loadingServers, setLoadingServers] = useState(false);
  const [loadingOus, setLoadingOus] = useState(false);
  const [ouLoadError, setOuLoadError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchingUsers, setSearchingUsers] = useState(false);
  const [searchedUsers, setSearchedUsers] = useState<AdUserSearchItem[]>([]);
  const [processingUserAction, setProcessingUserAction] = useState<string | null>(null);
  const [submittingCreate, setSubmittingCreate] = useState(false);
  const [submittingReset, setSubmittingReset] = useState(false);
  const [organizationalUnits, setOrganizationalUnits] = useState<AdOrganizationalUnit[]>([]);
  const [activeTab, setActiveTab] = useState<ActiveDirectoryTab>("users");

  const [createForm, setCreateForm] = useState<CreateAdUserRequest>({
    username: "",
    displayName: "",
    password: "",
    userPrincipalName: "",
    organizationalUnitPath: "",
    changePasswordAtLogon: true
  });

  const [resetUsername, setResetUsername] = useState("");
  const [resetForm, setResetForm] = useState<ResetAdUserPasswordRequest>({
    password: "",
    changePasswordAtLogon: true,
    enableAccount: true
  });

  const [lastCommand, setLastCommand] = useState<AgentCommand | null>(null);

  const selectedServer = useMemo(
    () => servers.find((item) => item.id === selectedServerId) ?? null,
    [servers, selectedServerId]
  );
  const commandStatusClass = lastCommand ? getStatusClass(lastCommand.status) : "status-pill status-unknown";
  const commandStatusLabel = lastCommand ? getCommandStatusText(lastCommand.status) : "Sem comando";
  const commandStatusDetailedLabel = lastCommand ? getCommandStatusText(lastCommand.status, true) : "Sem comando";
  const commandResultDescription = getCommandResultDescription(lastCommand?.resultOutput ?? null);
  const activeTabHelpText = useMemo(() => {
    if (activeTab === "users") {
      return "Busque usuários pelo nome/login e use as ações para bloquear, desbloquear ou enviar para redefinição de senha.";
    }

    if (activeTab === "create") {
      return "Preencha os dados obrigatórios para criar um novo usuário no Active Directory, com OU opcional.";
    }

    if (activeTab === "reset") {
      return "Informe o usuário e a nova senha para redefinir o acesso no Active Directory de forma controlada.";
    }

    return "Acompanhe o último comando executado, incluindo status, horário e resultado retornado pelo agente.";
  }, [activeTab]);

  async function loadServers() {
    setLoadingServers(true);
    try {
      const data = await apiRequest<ServerItem[]>("/api/servers", { token: auth.token });
      const adServers = data.filter((item) => item.supportsAd);
      setServers(adServers);
      setOuLoadError(null);
      if (!adServers.length) {
        setOrganizationalUnits([]);
        setSearchedUsers([]);
      }
      setSelectedServerId((current) => {
        if (!adServers.length) {
          return "";
        }

        if (current && adServers.some((item) => item.id === current)) {
          return current;
        }

        const defaultServer = adServers.find((item) => item.isDefault) ?? adServers[0];
        return defaultServer?.id ?? "";
      });
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar servidores AD.");
    } finally {
      setLoadingServers(false);
    }
  }

  async function loadOrganizationalUnits(serverId: string) {
    if (!serverId) {
      setOrganizationalUnits([]);
      setOuLoadError(null);
      return;
    }

    setLoadingOus(true);
    setOuLoadError(null);
    try {
      const data = await apiRequest<AdOrganizationalUnit[]>(`/api/ad/servers/${serverId}/organizational-units`, {
        token: auth.token
      });
      setOrganizationalUnits(data);
    } catch (error) {
      setOrganizationalUnits([]);
      setOuLoadError(error instanceof Error ? error.message : "Falha ao carregar OUs do AD.");
    } finally {
      setLoadingOus(false);
    }
  }

  async function refreshCommand(commandId: string) {
    try {
      const command = await apiRequest<AgentCommand>(`/api/agent-commands/${commandId}`, {
        token: auth.token
      });
      setLastCommand(command);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao atualizar status do comando.");
    }
  }

  useEffect(() => {
    void loadServers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  useEffect(() => {
    if (!selectedServerId) {
      setOrganizationalUnits([]);
      setOuLoadError(null);
      setSearchedUsers([]);
      setSearchQuery("");
      return;
    }

    setCreateForm((current) => ({ ...current, organizationalUnitPath: "" }));
    setSearchedUsers([]);
    void loadOrganizationalUnits(selectedServerId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedServerId, auth.token]);

  useEffect(() => {
    if (!lastCommand) {
      return;
    }

    const isFinal = lastCommand.status === "Succeeded" || lastCommand.status === "Failed";
    if (isFinal) {
      const marker = `${lastCommand.id}:${lastCommand.status}`;
      if (finalStatusToastRef.current !== marker) {
        finalStatusToastRef.current = marker;
        if (lastCommand.status === "Succeeded") {
          pushToast("success", "Comando AD executado com sucesso.");
        } else {
          pushToast("error", lastCommand.errorMessage ?? "Comando AD falhou no agent.");
        }
      }
      return;
    }

    const timer = window.setInterval(() => {
      void refreshCommand(lastCommand.id);
    }, 3000);

    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lastCommand?.id, lastCommand?.status, auth.token]);

  async function submitCreateUser(event: FormEvent) {
    event.preventDefault();
    if (!selectedServerId) {
      pushToast("error", "Selecione um servidor AD.");
      return;
    }

    if (!createForm.username.trim() || !createForm.password.trim()) {
      pushToast("error", "Username e senha são obrigatórios.");
      return;
    }

    setSubmittingCreate(true);
    try {
      const payload: CreateAdUserRequest = {
        username: createForm.username.trim(),
        displayName: createForm.displayName.trim(),
        password: createForm.password,
        userPrincipalName: createForm.userPrincipalName?.trim() || undefined,
        organizationalUnitPath: createForm.organizationalUnitPath?.trim() || undefined,
        changePasswordAtLogon: createForm.changePasswordAtLogon
      };

      const command = await apiRequest<AgentCommand>(`/api/ad/servers/${selectedServerId}/users`, {
        method: "POST",
        token: auth.token,
        body: payload
      });

      finalStatusToastRef.current = null;
      setLastCommand(command);
      setActiveTab("commands");
      pushToast("info", `Criação de usuário enfileirada. CommandId: ${command.id}`);
      setCreateForm((current) => ({
        ...current,
        password: ""
      }));
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao enfileirar criação de usuário.");
    } finally {
      setSubmittingCreate(false);
    }
  }

  async function submitResetPassword(event: FormEvent) {
    event.preventDefault();
    if (!selectedServerId) {
      pushToast("error", "Selecione um servidor AD.");
      return;
    }

    const normalizedUsername = resetUsername.trim();
    if (!normalizedUsername || !resetForm.password.trim()) {
      pushToast("error", "Username e nova senha são obrigatórios.");
      return;
    }

    setSubmittingReset(true);
    try {
      const command = await apiRequest<AgentCommand>(
        `/api/ad/servers/${selectedServerId}/users/${encodeURIComponent(normalizedUsername)}/reset-password`,
        {
          method: "POST",
          token: auth.token,
          body: resetForm
        }
      );

      finalStatusToastRef.current = null;
      setLastCommand(command);
      setActiveTab("commands");
      pushToast("info", `Reset de senha enfileirado. CommandId: ${command.id}`);
      setResetForm((current) => ({
        ...current,
        password: ""
      }));
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao enfileirar reset de senha.");
    } finally {
      setSubmittingReset(false);
    }
  }

  async function searchAdUsers(event: FormEvent) {
    event.preventDefault();
    if (!selectedServerId) {
      pushToast("error", "Selecione um servidor AD.");
      return;
    }

    const normalizedQuery = searchQuery.trim();
    if (normalizedQuery.length < 2) {
      pushToast("error", "Informe ao menos 2 caracteres para buscar.");
      return;
    }

    setSearchingUsers(true);
    try {
      const payload: SearchAdUsersRequest = {
        query: normalizedQuery,
        limit: 20
      };

      const result = await apiRequest<AdUserSearchItem[]>(`/api/ad/servers/${selectedServerId}/users/search`, {
        method: "POST",
        token: auth.token,
        body: payload
      });

      setSearchedUsers(result);
      if (result.length === 0) {
        pushToast("info", "Nenhum usuário AD encontrado para esse filtro.");
      }
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao buscar usuários AD.");
    } finally {
      setSearchingUsers(false);
    }
  }

  async function enqueueAdUserAction(username: string, action: "block" | "unblock") {
    if (!selectedServerId) {
      pushToast("error", "Selecione um servidor AD.");
      return;
    }

    const normalizedUsername = username.trim();
    if (!normalizedUsername) {
      pushToast("error", "Username AD inválido.");
      return;
    }

    const marker = `${action}:${normalizedUsername.toLowerCase()}`;
    setProcessingUserAction(marker);
    try {
      const command = await apiRequest<AgentCommand>(
        `/api/ad/servers/${selectedServerId}/users/${encodeURIComponent(normalizedUsername)}/${action}`,
        {
          method: "POST",
          token: auth.token
        }
      );

      finalStatusToastRef.current = null;
      setLastCommand(command);
      setActiveTab("commands");
      pushToast(
        "info",
        `${action === "block" ? "Bloqueio" : "Desbloqueio"} enfileirado para ${normalizedUsername}. CommandId: ${command.id}`
      );
    } catch (error) {
      pushToast(
        "error",
        error instanceof Error
          ? error.message
          : `Falha ao enfileirar ${action === "block" ? "bloqueio" : "desbloqueio"} AD.`
      );
    } finally {
      setProcessingUserAction(null);
    }
  }

  return (
    <section>
      <header className="page-header">
        <h2>Diretório Ativo (AD)</h2>
        <p>Gerencie usuários do AD com segurança, auditoria e execução controlada pelo agente.</p>
      </header>

      <div className="panel">
        <div className="toolbar">
          <label>
            Servidor AD
            <select value={selectedServerId} onChange={(event) => setSelectedServerId(event.target.value)}>
              {servers.map((server) => (
                <option key={server.id} value={server.id}>
                  {server.name} ({server.hostname})
                </option>
              ))}
            </select>
          </label>
          <button className="secondary-button" type="button" onClick={() => void loadServers()}>
            Atualizar
          </button>
        </div>

        {!servers.length && !loadingServers && <p>Nenhum servidor com capacidade AD habilitada.</p>}
        {loadingServers && <p>Atualizando lista de servidores AD...</p>}
        {selectedServer && (
          <p className="muted-text">
            Servidor selecionado: <strong>{selectedServer.name}</strong> | Agente{" "}
            <span className={selectedServer.isAgentOnline ? "status-pill status-online" : "status-pill status-offline"}>
              {selectedServer.isAgentOnline ? "Conectado" : "Sem comunicação"}
            </span>
          </p>
        )}
      </div>

      <div className="panel">
        <div className="ad-tabs" role="tablist" aria-label="Menu Active Directory">
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "users" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "users"}
            onClick={() => setActiveTab("users")}
          >
            Buscar e bloquear usuários
          </button>
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "create" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "create"}
            onClick={() => setActiveTab("create")}
          >
            Criar novo usuário
          </button>
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "reset" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "reset"}
            onClick={() => setActiveTab("reset")}
          >
            Redefinir senha de acesso
          </button>
          <button
            type="button"
            role="tab"
            className={`ad-tab-button ${activeTab === "commands" ? "ad-tab-button-active" : ""}`}
            aria-selected={activeTab === "commands"}
            onClick={() => setActiveTab("commands")}
          >
            Acompanhar comandos
            <span className={commandStatusClass}>
              {commandStatusLabel}
            </span>
          </button>
        </div>
        <p className="muted-text ad-tab-help">{activeTabHelpText}</p>
      </div>

      {activeTab === "users" && (
        <div className="panel ad-tab-panel">
          <h3>Buscar, Bloquear e Desbloquear</h3>
          <form className="toolbar" onSubmit={searchAdUsers}>
            <label>
              Buscar por username ou nome
              <input
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder="ex: maria, suporte, j.silva"
              />
            </label>
            <button className="primary-button" type="submit" disabled={searchingUsers || !selectedServerId}>
              {searchingUsers ? "Buscando..." : "Buscar usuários"}
            </button>
          </form>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Nome</th>
                  <th>Status AD</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {searchedUsers.map((user) => (
                  <tr key={user.username}>
                    <td>{user.username}</td>
                    <td>{user.displayName || "-"}</td>
                    <td>
                      <span className={getAdUserStatusClass(user)}>{getAdUserStatusLabel(user)}</span>
                    </td>
                    <td>
                      <div className="button-row">
                        <button
                          className="secondary-button"
                          type="button"
                          onClick={() => {
                            setResetUsername(user.username);
                            setActiveTab("reset");
                          }}
                        >
                          Usar em reset
                        </button>
                        <button
                          className="danger-button"
                          type="button"
                          disabled={processingUserAction !== null || !user.enabled}
                          onClick={() => void enqueueAdUserAction(user.username, "block")}
                        >
                          Bloquear
                        </button>
                        <button
                          className="primary-button"
                          type="button"
                          disabled={processingUserAction !== null || (user.enabled && !user.lockedOut)}
                          onClick={() => void enqueueAdUserAction(user.username, "unblock")}
                        >
                          Desbloquear
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {searchedUsers.length === 0 && (
                  <tr>
                    <td colSpan={4}>Nenhum usuário listado. Faça uma busca para visualizar resultados.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === "create" && (
        <div className="panel ad-tab-panel">
          <h3>Criar Usuário AD</h3>
          <form className="form-grid" onSubmit={submitCreateUser}>
            <label>
              Username (sAMAccountName)
              <input
                value={createForm.username}
                onChange={(event) => setCreateForm((current) => ({ ...current, username: event.target.value }))}
              />
            </label>
            <label>
              Nome de exibição
              <input
                value={createForm.displayName}
                onChange={(event) => setCreateForm((current) => ({ ...current, displayName: event.target.value }))}
              />
            </label>
            <label>
              Senha inicial
              <input
                type="password"
                value={createForm.password}
                onChange={(event) => setCreateForm((current) => ({ ...current, password: event.target.value }))}
              />
            </label>
            <label>
              UPN (opcional)
              <input
                value={createForm.userPrincipalName ?? ""}
                onChange={(event) => setCreateForm((current) => ({ ...current, userPrincipalName: event.target.value }))}
                placeholder="usuario@dominio.local"
              />
            </label>
            <label>
              OU (selecionar da estrutura)
              <div className="button-row">
                <select
                  value={createForm.organizationalUnitPath ?? ""}
                  onChange={(event) =>
                    setCreateForm((current) => ({ ...current, organizationalUnitPath: event.target.value }))
                  }
                >
                  <option value="">Padrão do domínio (sem OU específica)</option>
                  {organizationalUnits.map((item) => (
                    <option key={item.distinguishedName} value={item.distinguishedName}>
                      {formatOuOption(item)}
                    </option>
                  ))}
                </select>
                <button
                  className="secondary-button"
                  type="button"
                  onClick={() => void loadOrganizationalUnits(selectedServerId)}
                  disabled={!selectedServerId || loadingOus}
                >
                  {loadingOus ? "Atualizando OUs..." : "Atualizar OUs"}
                </button>
              </div>
              {ouLoadError && <span className="field-help error-text">{ouLoadError}</span>}
              {!ouLoadError && !loadingOus && organizationalUnits.length > 0 && (
                <span className="field-help">OUs disponíveis: {organizationalUnits.length}</span>
              )}
            </label>
            <label>
              OU path manual (opcional)
              <input
                value={createForm.organizationalUnitPath ?? ""}
                onChange={(event) =>
                  setCreateForm((current) => ({ ...current, organizationalUnitPath: event.target.value }))
                }
                placeholder="OU=Usuarios,DC=empresa,DC=local"
              />
              <span className="field-help">Pode editar manualmente se precisar de um DN específico.</span>
            </label>
            <label>
              <input
                type="checkbox"
                checked={createForm.changePasswordAtLogon}
                onChange={(event) =>
                  setCreateForm((current) => ({ ...current, changePasswordAtLogon: event.target.checked }))
                }
              />
              Forçar troca de senha no próximo logon
            </label>
            <button className="primary-button" type="submit" disabled={submittingCreate || !selectedServerId}>
              {submittingCreate ? "Enfileirando..." : "Criar usuário"}
            </button>
          </form>
        </div>
      )}

      {activeTab === "reset" && (
        <div className="panel ad-tab-panel">
          <h3>Redefinir Senha AD</h3>
          <form className="form-grid" onSubmit={submitResetPassword}>
            <label>
              Username (sAMAccountName)
              <input value={resetUsername} onChange={(event) => setResetUsername(event.target.value)} />
            </label>
            <label>
              Nova senha
              <input
                type="password"
                value={resetForm.password}
                onChange={(event) => setResetForm((current) => ({ ...current, password: event.target.value }))}
              />
            </label>
            <label>
              <input
                type="checkbox"
                checked={resetForm.changePasswordAtLogon}
                onChange={(event) =>
                  setResetForm((current) => ({ ...current, changePasswordAtLogon: event.target.checked }))
                }
              />
              Forçar troca de senha no próximo logon
            </label>
            <label>
              <input
                type="checkbox"
                checked={resetForm.enableAccount}
                onChange={(event) => setResetForm((current) => ({ ...current, enableAccount: event.target.checked }))}
              />
              Reativar conta após reset
            </label>
            <button className="primary-button" type="submit" disabled={submittingReset || !selectedServerId}>
              {submittingReset ? "Enfileirando..." : "Resetar senha"}
            </button>
          </form>
        </div>
      )}

      {activeTab === "commands" && (
        <div className="panel ad-tab-panel">
          <h3>Acompanhamento do Último Comando</h3>
          {!lastCommand && <p className="muted-text">Nenhum comando foi executado nesta sessão até o momento.</p>}
          {lastCommand && (
            <>
              <div className="button-row">
                <span className={getStatusClass(lastCommand.status)}>{commandStatusDetailedLabel}</span>
                <button className="secondary-button" type="button" onClick={() => void refreshCommand(lastCommand.id)}>
                  Atualizar status
                </button>
              </div>
              <p>
                <strong>ID do comando:</strong> <code>{lastCommand.id}</code>
              </p>
              <p>
                <strong>Solicitado em:</strong> {formatDateTime(lastCommand.requestedAtUtc)}
              </p>
              <p>
                <strong>Concluído em:</strong> {formatDateTime(lastCommand.completedAtUtc)}
              </p>
              {lastCommand.errorMessage && <p className="error-banner">{lastCommand.errorMessage}</p>}
              {commandResultDescription && <p className="info-banner">{commandResultDescription}</p>}
              {lastCommand.resultOutput && <pre className="command-output">{lastCommand.resultOutput}</pre>}
            </>
          )}
        </div>
      )}
    </section>
  );
}
