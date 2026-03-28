type ModalProps = {
  title: string;
  children: React.ReactNode;
  onClose: () => void;
};

export function Modal({ title, children, onClose }: ModalProps) {
  return (
    <div className="modal-overlay" role="presentation" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" aria-label={title} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{title}</h3>
          <button type="button" className="ghost-button" onClick={onClose}>
            Fechar
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
