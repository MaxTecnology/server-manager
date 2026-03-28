import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function makeClassName(isActive: boolean) {
  return isActive ? "nav-link nav-link-active" : "nav-link";
}

export function AppLayout() {
  const auth = useAuth();
  const navigate = useNavigate();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <p className="brand-eyebrow">Painel Interno</p>
          <h1>RDS Session Manager</h1>
        </div>

        <nav className="nav-list">
          <NavLink className={({ isActive }) => makeClassName(isActive)} to="/dashboard">
            Dashboard
          </NavLink>
          <NavLink className={({ isActive }) => makeClassName(isActive)} to="/sessions">
            Sessões
          </NavLink>
          <NavLink className={({ isActive }) => makeClassName(isActive)} to="/audit">
            Auditoria
          </NavLink>
          {auth.isAdministrator && (
            <>
              <NavLink className={({ isActive }) => makeClassName(isActive)} to="/settings">
                Configurações
              </NavLink>
              <NavLink className={({ isActive }) => makeClassName(isActive)} to="/users">
                Usuários e Perfis
              </NavLink>
            </>
          )}
        </nav>

        <div className="sidebar-footer">
          <p>
            Conectado como <strong>{auth.user?.displayName}</strong>
          </p>
          <button
            className="secondary-button"
            type="button"
            onClick={() => {
              auth.logout();
              navigate("/login", { replace: true });
            }}
          >
            Sair
          </button>
        </div>
      </aside>

      <main className="content-area">
        <Outlet />
      </main>
    </div>
  );
}
