import { createContext, useContext, useMemo, useState } from "react";

export type ToastMessage = {
  id: string;
  type: "success" | "error" | "info";
  text: string;
};

type ToastContextValue = {
  toasts: ToastMessage[];
  pushToast: (type: ToastMessage["type"], text: string) => void;
  removeToast: (id: string) => void;
};

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

function makeId() {
  if (typeof globalThis.crypto !== "undefined" && typeof globalThis.crypto.randomUUID === "function") {
    return globalThis.crypto.randomUUID();
  }

  return `toast-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const value = useMemo<ToastContextValue>(() => {
    return {
      toasts,
      pushToast(type, text) {
        const id = makeId();
        setToasts((current) => [...current, { id, type, text }]);
        window.setTimeout(() => {
          setToasts((current) => current.filter((item) => item.id !== id));
        }, 5000);
      },
      removeToast(id) {
        setToasts((current) => current.filter((item) => item.id !== id));
      }
    };
  }, [toasts]);

  return <ToastContext.Provider value={value}>{children}</ToastContext.Provider>;
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast deve ser usado dentro de ToastProvider.");
  }

  return context;
}
