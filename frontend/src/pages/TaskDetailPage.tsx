import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { taskApi } from '../api/taskApi';
import { projectApi } from '../api/projectApi';
import { useAuth } from '../features/auth/AuthContext';
import type { ProjectMember, TaskActivity, TaskAttachment, TaskItem } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

export function TaskDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const [task, setTask] = useState<TaskItem | null>(null);
  const [comment, setComment] = useState('');
  const [comments, setComments] = useState<string[]>([]);
  const [attachments, setAttachments] = useState<TaskAttachment[]>([]);
  const [attachmentError, setAttachmentError] = useState('');
  const [uploading, setUploading] = useState(false);
  const [activities, setActivities] = useState<TaskActivity[]>([]);
  const [nextStatus, setNextStatus] = useState('');
  const [savingStatus, setSavingStatus] = useState(false);
  const [pageError, setPageError] = useState('');
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [selectedAssigneeId, setSelectedAssigneeId] = useState('');
  const [savingAssignee, setSavingAssignee] = useState(false);
  const canManageTasks = user?.roles.includes('Admin') || user?.roles.includes('ProjectManager');

  const loadTask = async () => {
    if (!id) return;

    try {
      const [response, commentsResponse, attachmentsResponse, activityResponse] = await Promise.all([
        taskApi.getById(id),
        taskApi.getComments(id),
        taskApi.getAttachments(id),
        taskApi.getActivity(id),
      ]);
      setTask(response.data);
      setNextStatus(response.data.status);
      setSelectedAssigneeId(response.data.assignedToUserId ?? '');
      setComments(commentsResponse.data.map((entry) => entry.content));
      setAttachments(attachmentsResponse.data);
      setActivities(activityResponse.data);
      if (canManageTasks) setMembers((await projectApi.getMembers(response.data.projectId)).data);
    } catch (error) {
      console.error(error);
      setPageError('Görev bilgileri yüklenemedi veya bu göreve erişim izniniz yok.');
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
  }, [id, canManageTasks]);

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
      setActivities(await taskApi.getActivity(task.id).then((response) => response.data));
    } catch (error) {
      console.error('Add comment failed', error);
    }
  };

  const handleStatusSave = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!task || nextStatus === task.status) return;
    setSavingStatus(true);
    setPageError('');
    try {
      await taskApi.update(task.id, { status: nextStatus });
      await loadTask();
    } catch (error) {
      console.error(error);
      setPageError('Görev durumu güncellenemedi. İzin verilen durum geçişini kontrol edin.');
    } finally { setSavingStatus(false); }
  };

  const handleAssigneeSave = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!task || selectedAssigneeId === (task.assignedToUserId ?? '')) return;
    setSavingAssignee(true);
    setPageError('');
    try {
      await taskApi.update(task.id, selectedAssigneeId
        ? { assignedToUserId: selectedAssigneeId }
        : { clearAssignment: true });
      await loadTask();
    } catch (error) {
      console.error(error);
      setPageError('Görev sorumlusu güncellenemedi. Seçilen kullanıcının aktif proje üyesi olduğunu kontrol edin.');
    } finally { setSavingAssignee(false); }
  };

  if (!task) {
    return <div className="page-stack">Yükleniyor...</div>;
  }

  return (
    <div className="page-stack">
      {pageError && <div className="error-box">{pageError}</div>}
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
        {(user?.roles.includes('Admin') || user?.roles.includes('ProjectManager') || user?.roles.includes('TeamMember')) && <form className="status-update-form" onSubmit={handleStatusSave}>
          <label>Görev durumunu değiştir<select value={nextStatus} onChange={(event) => setNextStatus(event.target.value)}>
            <option value="New">Yeni</option><option value="InProgress">Devam ediyor</option><option value="Waiting">Beklemede</option><option value="Completed">Tamamlandı</option><option value="Cancelled">İptal</option>
          </select></label>
          <button type="submit" disabled={savingStatus || nextStatus === task.status}>{savingStatus ? 'Kaydediliyor...' : 'Durumu kaydet'}</button>
        </form>}
        {canManageTasks && <form className="status-update-form" onSubmit={handleAssigneeSave}>
          <label>Görev sorumlusu<select value={selectedAssigneeId} onChange={(event) => setSelectedAssigneeId(event.target.value)}>
            <option value="">Atanmamış</option>{members.filter((member) => member.isActive).map((member) => <option key={member.userId} value={member.userId}>{member.firstName || member.userName} {member.lastName ?? ''} ({member.email})</option>)}
          </select></label>
          <button type="submit" disabled={savingAssignee || selectedAssigneeId === (task.assignedToUserId ?? '')}>{savingAssignee ? 'Kaydediliyor...' : 'Atamayı kaydet'}</button>
        </form>}
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
        <div className="panel-header"><h3>Aktivite geçmişi</h3></div>
        {activities.length === 0 ? <div className="empty-state">Bu görev için henüz aktivite yok.</div> : <ul className="list-group">
          {activities.map((activity) => <li key={activity.id}>
            <strong>{activity.description || activity.action}</strong>
            <span>{activity.userName || 'Sistem'} · {formatTurkishDate(activity.createdAt)}</span>
          </li>)}
        </ul>}
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
