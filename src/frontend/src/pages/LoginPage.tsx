import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { authExpiredNoticeStorageKey, useAuth } from "../context/AuthContext";

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sessionNotice, setSessionNotice] = useState<string | null>(null);

  useEffect(() => {
    if (sessionStorage.getItem(authExpiredNoticeStorageKey) === "1") {
      setSessionNotice("Sua sessão expirou. Faça login novamente para continuar.");
      sessionStorage.removeItem(authExpiredNoticeStorageKey);
    }
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setSessionNotice(null);

    try {
      await auth.login(username, password);
      navigate("/dashboard", { replace: true });
    } catch (submissionError) {
      const message = submissionError instanceof Error ? submissionError.message : "Falha ao autenticar.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>Gerenciador de Sessões RDS</h1>
        <p>Faça login para administrar sessões com segurança.</p>
        <form onSubmit={onSubmit} className="form-grid">
          {sessionNotice && <div className="info-banner">{sessionNotice}</div>}
          <label>
            Usuário
            <input value={username} onChange={(event) => setUsername(event.target.value)} placeholder="usuario" />
          </label>
          <label>
            Senha
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="********"
            />
          </label>
          {error && <div className="error-banner">{error}</div>}
          <button className="primary-button" disabled={loading} type="submit">
            {loading ? "Entrando..." : "Entrar"}
          </button>
        </form>
      </div>
    </div>
  );
}
