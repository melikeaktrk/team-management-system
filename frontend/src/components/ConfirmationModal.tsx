import { useEffect } from 'react';

type ConfirmationModalProps = {
  isOpen: boolean;
  projectName: string;
  isLoading: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
};

export function ConfirmationModal({
  isOpen,
  projectName,
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
        aria-labelledby="delete-project-title"
        aria-describedby="delete-project-description"
      >
        <h2 id="delete-project-title">Projeyi silmek istiyor musunuz?</h2>
        <p id="delete-project-description">
          <strong>{projectName}</strong> projesini silmek istediğinize emin misiniz? Bu işlem projeyi ve ilişkili görev verilerini silebilir ve geri alınamaz.
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
            {isLoading ? 'Siliniyor...' : 'Projeyi sil'}
          </button>
        </div>
      </section>
    </div>
  );
}
