import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { projectApi } from '../api/projectApi';
import { taskApi } from '../api/taskApi';
import { useAuth } from '../features/auth/AuthContext';
import type { Project, TaskItem, TaskItemCreateRequest } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

const defaultForm: TaskItemCreateRequest = {
  projectId: '',
  title: '',
  description: '',
  priority: 'Medium',
  dueDate: '',
  assignedToUserId: null,
};

export function TasksPage() {
  const { user } = useAuth();
  const canCreateTasks = user?.roles.some(
    (role) => role === 'Admin' || role === 'ProjectManager'
  ) ?? false;
  const [projects, setProjects] = useState<Project[]>([]);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [form, setForm] = useState<TaskItemCreateRequest>(defaultForm);

  const projectOptions = useMemo(() => projects, [projects]);

  const loadData = async () => {
    try {
      const projectResponse = await projectApi.getAll();
      setProjects(projectResponse.data);

      if (projectResponse.data.length > 0) {
        const firstProjectId = projectResponse.data[0].id;
        const taskResponse = await taskApi.getByProject(firstProjectId);
        setTasks(taskResponse.data);
        setForm((prev) => ({ ...prev, projectId: firstProjectId }));
      }
    } catch (error) {
      console.error('Task page failed', error);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const handleSelectProject = async (projectId: string) => {
    setForm((prev) => ({ ...prev, projectId }));
    try {
      const response = await taskApi.getByProject(projectId);
      setTasks(response.data);
    } catch (error) {
      console.error(error);
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      await taskApi.create({
        ...form,
        description: form.description || undefined,
        dueDate: form.dueDate || null,
      });
      setForm((prev) => ({ ...prev, title: '', description: '', dueDate: '', priority: 'Medium' }));
      if (form.projectId) {
        const response = await taskApi.getByProject(form.projectId);
        setTasks(response.data);
      }
    } catch (error) {
      console.error(error);
    }
  };

  return (
    <div className="page-stack">
      {canCreateTasks && <section className="panel">
        <div className="panel-header">
          <h3>Yeni görev</h3>
        </div>

        <form onSubmit={handleSubmit} className="form-grid">
          <label>
            Proje
            <select
              value={form.projectId}
              onChange={(e) => handleSelectProject(e.target.value)}
            >
              <option value="">Lütfen seçiniz...</option>
              {projectOptions.map((project) => (
                <option key={project.id} value={project.id}>
                  {project.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            Başlık
            <input
              value={form.title}
              onChange={(e) => setForm((prev) => ({ ...prev, title: e.target.value }))}
              required
            />
          </label>

          <label>
            Açıklama
            <textarea
              value={form.description}
              onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
            />
          </label>

          <div className="two-column">
            <label>
              Öncelik
              <select
                value={form.priority}
                onChange={(e) => setForm((prev) => ({ ...prev, priority: e.target.value }))}
              >
                <option value="">Lütfen seçiniz...</option>
                <option value="Low">Düşük</option>
                <option value="Medium">Orta</option>
                <option value="High">Yüksek</option>
              </select>
            </label>

            <label>
              Bitiş tarihi
              <input
                type="date"
                value={form.dueDate ?? ''}
                onChange={(e) => setForm((prev) => ({ ...prev, dueDate: e.target.value }))}
              />
            </label>
          </div>

          <button type="submit">Görev ekle</button>
        </form>
      </section>}

      <section className="panel">
        <div className="panel-header">
          <h3>Görev listesi</h3>
        </div>

        <div className="card-list">
          {tasks.length === 0 ? (
            <div className="empty-state">Henüz veri bulunmuyor.</div>
          ) : (
            tasks.map((task) => (
              <Link to={`/tasks/${task.id}`} key={task.id} className="mini-card wide">
                <h4>{task.title}</h4>
                <p>{task.description || 'Açıklama yok.'}</p>
                <div className="meta-line">
                  <span>{task.status}</span>
                  <span>{task.priority}</span>
                  <span>{formatTurkishDate(task.dueDate)}</span>
                </div>
              </Link>
            ))
          )}
        </div>
      </section>
    </div>
  );
}
