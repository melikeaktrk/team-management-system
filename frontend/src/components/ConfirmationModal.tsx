import { useEffect } from 'react';

type ConfirmationModalProps = {
  isOpen: boolean;
  projectName?: string;
  title?: string;
  description?: React.ReactNode;
  confirmLabel?: string;
  loadingLabel?: string;
  isLoading: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
};

export function ConfirmationModal({
  isOpen,
  projectName,
  title = 'Projeyi silmek istiyor musunuz?',
  description,
  confirmLabel = 'Projeyi sil',
  loadingLabel = 'Siliniyor...',
  isLoading,
  error,
  onConfirm,
  onCancel,
}: ConfirmationModalProps) {
  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isLoading) onCancel();
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, isLoading, onCancel]);

  if (!isOpen) return null;

  return (
    <div
      className="confirmation-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isLoading) onCancel();
      }}
    >
      <section
        className="confirmation-modal"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirmation-title"
        aria-describedby="confirmation-description"
      >
        <h2 id="confirmation-title">{title}</h2>
        <p id="confirmation-description">
          {description ?? <><strong>{projectName}</strong> projesini silmek istediğinize emin misiniz? Bu işlem projeyi ve ilişkili görev verilerini silebilir ve geri alınamaz.</>}
        </p>
        {error && <p className="confirmation-modal-error" role="alert">{error}</p>}
        <div className="confirmation-modal-actions">
          <button type="button" onClick={onCancel} disabled={isLoading} autoFocus>
            Vazgeç
          </button>
          <button
            type="button"
            className="confirmation-danger"
            onClick={onConfirm}
            disabled={isLoading}
          >
            {isLoading ? loadingLabel : confirmLabel}
          </button>
        </div>
      </section>
    </div>
  );
}
