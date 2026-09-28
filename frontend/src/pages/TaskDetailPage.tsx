import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { taskApi } from '../api/taskApi';
import type { TaskAttachment, TaskItem } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

export function TaskDetailPage() {
  const { id } = useParams();
  const [task, setTask] = useState<TaskItem | null>(null);
  const [comment, setComment] = useState('');
  const [comments, setComments] = useState<string[]>([]);
  const [attachments, setAttachments] = useState<TaskAttachment[]>([]);
  const [attachmentError, setAttachmentError] = useState('');
  const [uploading, setUploading] = useState(false);

  const loadTask = async () => {
    if (!id) return;

    try {
      const [response, commentsResponse, attachmentsResponse] = await Promise.all([
        taskApi.getById(id),
        taskApi.getComments(id),
        taskApi.getAttachments(id),
      ]);
      setTask(response.data);
      setComments(commentsResponse.data.map((entry) => entry.content));
      setAttachments(attachmentsResponse.data);
    } catch (error) {
      console.error(error);
    }
  };

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file || !task) return;

    try {
      setUploading(true);
      setAttachmentError('');
      await taskApi.uploadAttachment(task.id, file);
      setAttachments((await taskApi.getAttachments(task.id)).data);
      event.target.value = '';
    } catch (error) {
      console.error('Attachment upload failed', error);
      setAttachmentError('Dosya yüklenemedi. PDF, PNG, JPG veya TXT (en fazla 10 MB) seçin.');
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (attachment: TaskAttachment) => {
    try {
      const response = await taskApi.downloadAttachment(attachment.id);
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.originalFileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Attachment download failed', error);
      setAttachmentError('Dosya indirilemedi.');
    }
  };

  useEffect(() => {
    void loadTask();
  }, [id]);

  const handleAddComment = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!id || !comment.trim() || !task) return;

    try {
      await taskApi.addComment(task.id, {
        taskItemId: task.id,
        content: comment.trim(),
      });
      setComments((prev) => [comment.trim(), ...prev]);
      setComment('');
    } catch (error) {
      console.error('Add comment failed', error);
    }
  };

  if (!task) {
    return <div className="page-stack">Yükleniyor...</div>;
  }

  return (
    <div className="page-stack">
      <section className="panel">
        <div className="panel-header">
          <h3>{task.title}</h3>
        </div>
        <p>{task.description || 'Açıklama yok.'}</p>
        <div className="meta-line">
          <span>Statü: {task.status}</span>
          <span>Öncelik: {task.priority}</span>
          <span>Son tarih: {formatTurkishDate(task.dueDate)}</span>
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h3>Dosyalar</h3>
        </div>
        <label>
          Dosya ekle (PDF, PNG, JPG, TXT; en fazla 10 MB)
          <input
            type="file"
            accept=".pdf,.png,.jpg,.jpeg,.txt"
            onChange={(event) => void handleUpload(event)}
            disabled={uploading}
          />
        </label>
        {uploading && <p>Yükleniyor...</p>}
        {attachmentError && <div className="error-box">{attachmentError}</div>}
        <ul className="list-group">
          {attachments.length === 0 ? (
            <li className="empty-state">Henüz dosya eklenmemiş.</li>
          ) : attachments.map((attachment) => (
            <li key={attachment.id}>
              <button type="button" onClick={() => void handleDownload(attachment)}>
                {attachment.originalFileName}
              </button>
              <span>{Math.ceil(attachment.fileSize / 1024)} KB</span>
            </li>
          ))}
        </ul>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h3>Yorumlar</h3>
        </div>

        <form onSubmit={handleAddComment} className="comment-form">
          <textarea
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            placeholder="Yorum ekleyin..."
          />
          <button type="submit" disabled={!comment.trim()}>
            Yorum ekle
          </button>
        </form>

        <ul className="list-group">
          {comments.length === 0 ? (
            <li className="empty-state">Henüz yorum yok.</li>
          ) : (
            comments.map((entry, index) => (
              <li key={`${entry}-${index}`}>
                <strong>Yorum</strong>
                <span>{entry}</span>
              </li>
            ))
          )}
        </ul>
      </section>
    </div>
  );
}
