type ApiOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  token?: string | null;
  body?: unknown;
};

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
type AuthFailureHandler = () => void;

let authFailureHandler: AuthFailureHandler | null = null;
let authFailureNotified = false;

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = "ApiError";
  }
}

export function setAuthFailureHandler(handler: AuthFailureHandler | null) {
  authFailureHandler = handler;
  if (!handler) {
    authFailureNotified = false;
  }
}

export function resetAuthFailureFlag() {
  authFailureNotified = false;
}

export async function apiRequest<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: options.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  if (!response.ok) {
    let message = "Erro na requisição.";
    try {
      const payload = (await response.json()) as { message?: string };
      if (payload.message) {
        message = payload.message;
      }
    } catch {
      message = response.statusText || message;
    }

    const isAuthFailure = response.status === 401;
    if (isAuthFailure && options.token && authFailureHandler && !authFailureNotified) {
      authFailureNotified = true;
      authFailureHandler();
    }

    throw new ApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  var contentType = response.headers.get("content-type") ?? "";
  if (!contentType.toLowerCase().includes("application/json")) {
    throw new Error("Resposta inesperada do servidor. Verifique se o backend foi publicado.");
  }

  try {
    return (await response.json()) as T;
  } catch {
    throw new Error("Resposta JSON inválida recebida da API.");
  }
}
