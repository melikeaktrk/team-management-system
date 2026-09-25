import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { taskApi } from '../api/taskApi';
import type { TaskItem } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

export function TaskDetailPage() {
  const { id } = useParams();
  const [task, setTask] = useState<TaskItem | null>(null);
  const [comment, setComment] = useState('');
  const [comments, setComments] = useState<string[]>([]);

  const loadTask = async () => {
    if (!id) return;

    try {
      const response = await taskApi.getById(id);
      setTask(response.data);
    } catch (error) {
      console.error(error);
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
