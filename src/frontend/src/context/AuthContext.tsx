import { createContext, useContext, useMemo, useState } from "react";
import { apiRequest } from "../api/client";
import type { LoginResponse, UserSummary } from "../types";

type AuthState = {
  token: string | null;
  user: UserSummary | null;
};

type AuthContextValue = AuthState & {
  isAuthenticated: boolean;
  isAdministrator: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
};

const storageKey = "session-manager-auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStorage(): AuthState {
  const raw = localStorage.getItem(storageKey);
  if (!raw) {
    return { token: null, user: null };
  }

  try {
    return JSON.parse(raw) as AuthState;
  } catch {
    localStorage.removeItem(storageKey);
    return { token: null, user: null };
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<AuthState>(() => readStorage());

  const value = useMemo<AuthContextValue>(() => {
    return {
      ...state,
      isAuthenticated: Boolean(state.token && state.user),
      isAdministrator: state.user?.roles.includes("Administrator") ?? false,
      async login(username: string, password: string) {
        const response = await apiRequest<LoginResponse>("/api/auth/login", {
          method: "POST",
          body: { username, password }
        });

        const nextState: AuthState = {
          token: response.accessToken,
          user: response.user
        };

        localStorage.setItem(storageKey, JSON.stringify(nextState));
        setState(nextState);
      },
      logout() {
        localStorage.removeItem(storageKey);
        setState({ token: null, user: null });
      }
    };
  }, [state]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth deve ser usado dentro de AuthProvider.");
  }

  return context;
}
