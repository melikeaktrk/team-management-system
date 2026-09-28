import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { notificationApi } from '../api/notificationApi';
import { projectApi } from '../api/projectApi';
import type { NotificationItem, Project } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

export function DashboardPage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const loadData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [projectsResponse, notificationsResponse] = await Promise.all([
          projectApi.getAll(),
          notificationApi.getAll(),
        ]);

        if (isMounted) {
          setProjects(projectsResponse.data || []);
          setNotifications(notificationsResponse.data || []);
        }
      } catch (err) {
        if (isMounted) {
          console.error('Dashboard fetch failed', err);
          setError('Veriler yüklenirken bir sorun oluştu.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    void loadData();

    return () => {
      isMounted = false;
    };
  }, []);

  // Durum rozeti renk sınıfı belirleyici
  const getStatusBadge = (status: string) => {
    const statusLower = status?.toLowerCase() || '';
    if (statusLower === 'active' || statusLower === 'aktif') return 'status-badge active';
    if (statusLower === 'completed' || statusLower === 'tamamlandı') return 'status-badge completed';
    return 'status-badge pending';
  };

  if (loading) {
    return (
      <div className="page-stack">
        <div className="loading-state">
          <div className="spinner"></div>
          <span>Veriler yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="page-stack">
        <div className="error-box">{error}</div>
      </div>
    );
  }

  const activeProjectsCount = projects.filter(
    (p) => p.status === 'InProgress'
  ).length;

  return (
    <div className="page-stack">
      {/* İstatistik Kartları */}
      <section className="stats-grid">
        <div className="stat-card accent">
          <span>Toplam Proje</span>
          <strong>{projects.length}</strong>
        </div>
        <div className="stat-card">
          <span>Devam Eden Proje</span>
          <strong>{activeProjectsCount}</strong>
        </div>
        <div className="stat-card">
          <span>Bildirimler</span>
          <strong>{notifications.length}</strong>
        </div>
      </section>

      {/* Son Projeler Paneli */}
      <section className="panel">
        <div className="panel-header">
          <h3>Projeler</h3>
          <Link to="/projects" className="panel-link">Tümünü gör →</Link>
        </div>

        <div className="card-list">
          {projects.length === 0 ? (
            <div className="empty-state">Henüz eklenmiş bir proje bulunmuyor.</div>
          ) : (
            projects.slice(0, 3).map((project) => (
              <Link to={`/projects/${project.id}`} key={project.id} className="mini-card">
                <div className="card-top">
                  <h4>{project.name}</h4>
                  <span className={getStatusBadge(project.status)}>
                    {project.status}
                  </span>
                </div>
                <p>{project.description || 'Açıklama eklenmemiş.'}</p>
                <div className="meta-line">
                  <span>Son Tarih: {formatTurkishDate(project.dueDate)}</span>
                </div>
              </Link>
            ))
          )}
        </div>
      </section>

      {/* Son Bildirimler Paneli */}
      <section className="panel">
        <div className="panel-header">
          <h3>Son Bildirimler</h3>
          <Link to="/notifications" className="panel-link">Hepsini gör →</Link>
        </div>

        <ul className="list-group">
          {notifications.length === 0 ? (
            <li className="empty-state">Henüz okunmamış bildiriminiz yok.</li>
          ) : (
            notifications.slice(0, 5).map((notification) => (
              <li key={notification.id} className="notification-item">
                <div className="notification-content">
                  <strong>{notification.title}</strong>
                  <p>{notification.message}</p>
                </div>
              </li>
            ))
          )}
        </ul>
      </section>
    </div>
  );
}
