import { FormEvent, useEffect, useState } from "react";
import { apiRequest } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import type { UserItem } from "../types";

type NewUserForm = {
  username: string;
  displayName: string;
  password: string;
  roles: string[];
};

export function UsersPage() {
  const auth = useAuth();
  const { pushToast } = useToast();
  const [users, setUsers] = useState<UserItem[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [passwordDrafts, setPasswordDrafts] = useState<Record<string, string>>({});
  const [form, setForm] = useState<NewUserForm>({
    username: "",
    displayName: "",
    password: "",
    roles: []
  });

  async function load() {
    try {
      const [userData, roleData] = await Promise.all([
        apiRequest<UserItem[]>("/api/users", { token: auth.token }),
        apiRequest<string[]>("/api/users/roles", { token: auth.token })
      ]);

      setUsers(userData);
      setRoles(roleData);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao carregar usuários.");
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.token]);

  async function createUser(event: FormEvent) {
    event.preventDefault();
    try {
      await apiRequest("/api/users", {
        method: "POST",
        token: auth.token,
        body: form
      });
      setForm({
        username: "",
        displayName: "",
        password: "",
        roles: []
      });
      pushToast("success", "Usuário criado com sucesso.");
      await load();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao criar usuário.");
    }
  }

  async function toggleUserStatus(user: UserItem) {
    try {
      await apiRequest(`/api/users/${user.id}/status`, {
        method: "PATCH",
        token: auth.token,
        body: { isActive: !user.isActive }
      });
      pushToast("success", `Status do usuário ${user.username} atualizado.`);
      await load();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao atualizar status.");
    }
  }

  async function setUserRoles(user: UserItem, nextRoles: string[]) {
    try {
      await apiRequest(`/api/users/${user.id}/roles`, {
        method: "PATCH",
        token: auth.token,
        body: { roles: nextRoles }
      });
      pushToast("success", `Perfis de ${user.username} atualizados.`);
      await load();
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao atualizar perfis.");
    }
  }

  async function setUserPassword(user: UserItem) {
    const nextPassword = passwordDrafts[user.id] ?? "";
    if (!nextPassword || nextPassword.length < 8) {
      pushToast("error", "A nova senha deve ter no mÃ­nimo 8 caracteres.");
      return;
    }

    try {
      await apiRequest(`/api/users/${user.id}/password`, {
        method: "PATCH",
        token: auth.token,
        body: { password: nextPassword }
      });
      setPasswordDrafts((current) => ({ ...current, [user.id]: "" }));
      pushToast("success", `Senha de ${user.username} atualizada.`);
    } catch (error) {
      pushToast("error", error instanceof Error ? error.message : "Falha ao atualizar senha.");
    }
  }

  return (
    <section>
      <header className="page-header">
        <h2>Usuários e Perfis</h2>
        <p>Gestão de contas e permissões de acesso ao painel.</p>
      </header>

      <div className="panel">
        <h3>Novo Usuário</h3>
        <form className="form-grid" onSubmit={createUser}>
          <label>
            Username
            <input
              value={form.username}
              onChange={(event) => setForm((current) => ({ ...current, username: event.target.value }))}
            />
          </label>
          <label>
            Nome de Exibição
            <input
              value={form.displayName}
              onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))}
            />
          </label>
          <label>
            Senha
            <input
              type="password"
              value={form.password}
              onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
            />
          </label>
          <div>
            <p>Perfis</p>
            <div className="checkbox-row">
              {roles.map((role) => (
                <label key={role}>
                  <input
                    type="checkbox"
                    checked={form.roles.includes(role)}
                    onChange={(event) =>
                      setForm((current) => ({
                        ...current,
                        roles: event.target.checked
                          ? [...current.roles, role]
                          : current.roles.filter((item) => item !== role)
                      }))
                    }
                  />
                  {role}
                </label>
              ))}
            </div>
          </div>
          <button className="primary-button" type="submit">
            Criar Usuário
          </button>
        </form>
      </div>

      <div className="panel">
        <h3>Usuários Cadastrados</h3>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Nome</th>
                <th>Status</th>
                <th>Perfis</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.username}</td>
                  <td>{user.displayName}</td>
                  <td>{user.isActive ? "Ativo" : "Inativo"}</td>
                  <td>
                    <div className="checkbox-row">
                      {roles.map((role) => {
                        const checked = user.roles.includes(role);
                        return (
                          <label key={`${user.id}-${role}`}>
                            <input
                              type="checkbox"
                              checked={checked}
                              onChange={(event) => {
                                const nextRoles = event.target.checked
                                  ? [...user.roles, role]
                                  : user.roles.filter((item) => item !== role);
                                void setUserRoles(user, nextRoles);
                              }}
                            />
                            {role}
                          </label>
                        );
                      })}
                    </div>
                  </td>
                  <td>
                    <div className="button-row">
                      <button className="secondary-button" type="button" onClick={() => void toggleUserStatus(user)}>
                        {user.isActive ? "Inativar" : "Ativar"}
                      </button>
                      <input
                        type="password"
                        value={passwordDrafts[user.id] ?? ""}
                        onChange={(event) =>
                          setPasswordDrafts((current) => ({ ...current, [user.id]: event.target.value }))
                        }
                        placeholder="Nova senha"
                      />
                      <button className="ghost-button" type="button" onClick={() => void setUserPassword(user)}>
                        Trocar senha
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!users.length && (
                <tr>
                  <td colSpan={5}>Nenhum usuário cadastrado.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
