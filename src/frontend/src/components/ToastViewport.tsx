import { useToast } from "../context/ToastContext";

export function ToastViewport() {
  const { toasts, removeToast } = useToast();

  return (
    <div className="toast-viewport">
      {toasts.map((toast) => (
        <button
          type="button"
          key={toast.id}
          className={`toast toast-${toast.type}`}
          onClick={() => removeToast(toast.id)}
          aria-label="Fechar notificação"
        >
          {toast.text}
        </button>
      ))}
    </div>
  );
}
