import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { projectApi } from '../api/projectApi';
import { taskApi } from '../api/taskApi';
import { useAuth } from '../features/auth/AuthContext';
import type { Project, ProjectMember, TaskItem, TaskItemCreateRequest } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

const defaultForm: TaskItemCreateRequest = {
  projectId: '', title: '', description: '', priority: 'Medium', dueDate: '', assignedToUserId: null,
};

export function TasksPage() {
  const { user } = useAuth();
  const canManageTasks = user?.roles.some((role) => role === 'Admin' || role === 'ProjectManager') ?? false;
  const [projects, setProjects] = useState<Project[]>([]);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [form, setForm] = useState<TaskItemCreateRequest>(defaultForm);
  const [filters, setFilters] = useState({ status: '', priority: '', assignedToUserId: '', dueFrom: '', dueTo: '' });
  const [sortBy, setSortBy] = useState('DueDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const projectOptions = useMemo(() => projects, [projects]);

  const loadTasks = async (projectId: string, page = pageNumber, filterValues = filters, pageLimit = pageSize, orderBy = sortBy, orderDirection = sortDirection) => {
    if (!projectId) { setTasks([]); setTotalCount(0); setTotalPages(0); return; }
    setLoading(true);
    setError('');
    try {
      const response = await taskApi.searchByProject(projectId, {
        ...filterValues, pageNumber: page, pageSize: pageLimit, sortBy: orderBy, sortDirection: orderDirection,
      });
      setTasks(response.data.items);
      setPageNumber(response.data.pageNumber);
      setTotalCount(response.data.totalCount);
      setTotalPages(response.data.totalPages);
    } catch (requestError) {
      console.error(requestError);
      setError('Görevler yüklenemedi. Proje erişiminizi ve API bağlantısını kontrol edin.');
      setTasks([]);
    } finally { setLoading(false); }
  };

  const loadMembers = async (projectId: string) => {
    if (!projectId || !canManageTasks) { setMembers([]); return; }
    try { setMembers((await projectApi.getMembers(projectId)).data); }
    catch (requestError) { console.error(requestError); setMembers([]); }
  };

  const loadProjects = async () => {
    try {
      const response = await projectApi.getAll();
      setProjects(response.data);
      const initialProjectId = response.data[0]?.id ?? '';
      setForm((current) => ({ ...current, projectId: initialProjectId }));
      if (initialProjectId) {
        await Promise.all([loadTasks(initialProjectId, 1), loadMembers(initialProjectId)]);
      }
    } catch (requestError) {
      console.error(requestError);
      setError('Projeler yüklenemedi.');
    }
  };

  useEffect(() => { void loadProjects(); }, []);

  const handleSelectProject = async (projectId: string) => {
    setForm((current) => ({ ...current, projectId, assignedToUserId: null }));
    setPageNumber(1);
    await Promise.all([loadTasks(projectId, 1), loadMembers(projectId)]);
  };

  const handleSearch = async (event: React.FormEvent) => {
    event.preventDefault();
    await loadTasks(form.projectId, 1);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    try {
      await taskApi.create({ ...form, description: form.description || undefined, dueDate: form.dueDate || null });
      setForm((current) => ({ ...current, title: '', description: '', dueDate: '', priority: 'Medium', assignedToUserId: null }));
      await loadTasks(form.projectId, 1);
    } catch (requestError) {
      console.error(requestError);
      setError('Görev oluşturulamadı. Başlığı, proje üyeliğini ve bitiş tarihini kontrol edin.');
    }
  };

  return (
    <div className="page-stack">
      {canManageTasks && <section className="panel">
        <div className="panel-header"><h3>Yeni görev</h3></div>
        <form onSubmit={handleSubmit} className="form-grid">
          <label>Proje<select value={form.projectId} onChange={(event) => void handleSelectProject(event.target.value)} required>
            <option value="">Lütfen seçiniz...</option>
            {projectOptions.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
          </select></label>
          <label>Başlık<input value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} required maxLength={200} /></label>
          <label>Açıklama<textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} maxLength={4000} /></label>
          <div className="two-column">
            <label>Öncelik<select value={form.priority} onChange={(event) => setForm((current) => ({ ...current, priority: event.target.value }))}>
              <option value="Low">Düşük</option><option value="Medium">Orta</option><option value="High">Yüksek</option>
            </select></label>
            <label>Bitiş tarihi<input type="date" value={form.dueDate ?? ''} onChange={(event) => setForm((current) => ({ ...current, dueDate: event.target.value }))} /></label>
          </div>
          <label>Sorumlu üye<select value={form.assignedToUserId ?? ''} onChange={(event) => setForm((current) => ({ ...current, assignedToUserId: event.target.value || null }))}>
            <option value="">Atanmamış</option>
            {members.filter((member) => member.isActive).map((member) => <option key={member.userId} value={member.userId}>{member.firstName || member.userName} {member.lastName ?? ''} ({member.email})</option>)}
          </select></label>
          {error && <div className="error-box">{error}</div>}
          <button type="submit" disabled={!form.projectId}>Görev ekle</button>
        </form>
      </section>}

      <section className="panel">
        <div className="panel-header"><h3>Görev listesi</h3><span>{totalCount} görev</span></div>
        <form className="form-grid task-filter-grid" onSubmit={handleSearch}>
          <label>Proje<select value={form.projectId} onChange={(event) => void handleSelectProject(event.target.value)}>
            <option value="">Proje seçin</option>{projectOptions.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
          </select></label>
          <div className="two-column">
            <label>Durum<select value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value }))}>
              <option value="">Tüm durumlar</option><option value="New">Yeni</option><option value="InProgress">Devam ediyor</option><option value="Waiting">Beklemede</option><option value="Completed">Tamamlandı</option><option value="Cancelled">İptal</option>
            </select></label>
            <label>Öncelik<select value={filters.priority} onChange={(event) => setFilters((current) => ({ ...current, priority: event.target.value }))}>
              <option value="">Tüm öncelikler</option><option value="Low">Düşük</option><option value="Medium">Orta</option><option value="High">Yüksek</option>
            </select></label>
          </div>
          {canManageTasks && <label>Sorumlu üye<select value={filters.assignedToUserId} onChange={(event) => setFilters((current) => ({ ...current, assignedToUserId: event.target.value }))}>
            <option value="">Tüm üyeler</option>{members.map((member) => <option key={member.userId} value={member.userId}>{member.userName || member.email}</option>)}
          </select></label>}
          <div className="two-column">
            <label>Bitiş tarihi başlangıç<input type="date" value={filters.dueFrom} onChange={(event) => setFilters((current) => ({ ...current, dueFrom: event.target.value }))} /></label>
            <label>Bitiş tarihi bitiş<input type="date" value={filters.dueTo} onChange={(event) => setFilters((current) => ({ ...current, dueTo: event.target.value }))} /></label>
          </div>
          <div className="filter-actions">
            <label>Sıralama<select value={sortBy} onChange={(event) => setSortBy(event.target.value)}><option value="DueDate">Bitiş tarihi</option><option value="Title">Başlık</option><option value="Status">Durum</option><option value="Priority">Öncelik</option><option value="CreatedAt">Oluşturulma</option></select></label>
            <label>Yön<select value={sortDirection} onChange={(event) => setSortDirection(event.target.value as 'asc' | 'desc')}><option value="asc">Artan</option><option value="desc">Azalan</option></select></label>
            <label>Sayfa boyutu<select value={pageSize} onChange={(event) => setPageSize(Number(event.target.value))}><option value={5}>5</option><option value={10}>10</option><option value={25}>25</option><option value={50}>50</option></select></label>
          </div>
          <div className="filter-actions"><button type="submit">Filtrele ve sırala</button><button type="button" onClick={() => { const cleared = { status: '', priority: '', assignedToUserId: '', dueFrom: '', dueTo: '' }; setFilters(cleared); setSortBy('DueDate'); setSortDirection('asc'); void loadTasks(form.projectId, 1, cleared, pageSize, 'DueDate', 'asc'); }}>Filtreleri temizle</button></div>
        </form>
        {error && <div className="error-box">{error}</div>}
        {loading ? <div className="loading-state">Görevler yükleniyor...</div> : tasks.length === 0 ? <div className="empty-state">Bu filtrelerle görev bulunamadı.</div> : (
          <div className="card-list">
            {tasks.map((task) => <Link to={`/tasks/${task.id}`} key={task.id} className="mini-card wide">
              <h4>{task.title}</h4><p>{task.description || 'Açıklama yok.'}</p>
              <div className="meta-line"><span>{task.status}</span><span>{task.priority}</span><span>{formatTurkishDate(task.dueDate)}</span></div>
            </Link>)}
          </div>
        )}
        <div className="pagination-controls">
          <button type="button" disabled={pageNumber <= 1 || loading} onClick={() => void loadTasks(form.projectId, pageNumber - 1)}>Önceki</button>
          <span>Sayfa {totalPages === 0 ? 0 : pageNumber} / {totalPages}</span>
          <button type="button" disabled={pageNumber >= totalPages || loading} onClick={() => void loadTasks(form.projectId, pageNumber + 1)}>Sonraki</button>
        </div>
      </section>
    </div>
  );
}
