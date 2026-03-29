import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { AgentCommand, CreateAdUserRequest, ResetAdUserPasswordRequest, ServerItem } from "../types";

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

export function ActiveDirectoryPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const finalStatusToastRef = useRef<string | null>(null);

  const [servers, setServers] = useState<ServerItem[]>([]);
  const [selectedServerId, setSelectedServerId] = useState<string>("");
  const [loadingServers, setLoadingServers] = useState(false);
  const [submittingCreate, setSubmittingCreate] = useState(false);
  const [submittingReset, setSubmittingReset] = useState(false);

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

  async function loadServers() {
    setLoadingServers(true);
    try {
      const data = await apiRequest<ServerItem[]>("/api/servers", { token: auth.token });
      const adServers = data.filter((item) => item.supportsAd);
      setServers(adServers);
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

  return (
    <section>
      <header className="page-header">
        <h2>Active Directory</h2>
        <p>Operações de usuários AD via agent com auditoria e fila de execução.</p>
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
            Servidor selecionado: <strong>{selectedServer.name}</strong> | Agent{" "}
            <span className={selectedServer.isAgentOnline ? "status-pill status-online" : "status-pill status-offline"}>
              {selectedServer.isAgentOnline ? "Online" : "Offline"}
            </span>
          </p>
        )}
      </div>

      <div className="panel">
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
            OU path (opcional)
            <input
              value={createForm.organizationalUnitPath ?? ""}
              onChange={(event) =>
                setCreateForm((current) => ({ ...current, organizationalUnitPath: event.target.value }))
              }
              placeholder="OU=Usuarios,DC=empresa,DC=local"
            />
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

      <div className="panel">
        <h3>Resetar Senha AD</h3>
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

      {lastCommand && (
        <div className="panel">
          <h3>Último Comando AD</h3>
          <div className="button-row">
            <span className={getStatusClass(lastCommand.status)}>{lastCommand.status}</span>
            <button className="secondary-button" type="button" onClick={() => void refreshCommand(lastCommand.id)}>
              Atualizar status
            </button>
          </div>
          <p>
            <strong>CommandId:</strong> <code>{lastCommand.id}</code>
          </p>
          <p>
            <strong>Solicitado em:</strong> {formatDateTime(lastCommand.requestedAtUtc)}
          </p>
          <p>
            <strong>Concluído em:</strong> {formatDateTime(lastCommand.completedAtUtc)}
          </p>
          {lastCommand.errorMessage && <p className="error-banner">{lastCommand.errorMessage}</p>}
          {lastCommand.resultOutput && <pre className="command-output">{lastCommand.resultOutput}</pre>}
        </div>
      )}
    </section>
  );
}
