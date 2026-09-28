import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { projectApi } from '../api/projectApi';
import { taskApi } from '../api/taskApi';
import { useAuth } from '../features/auth/AuthContext';
import type {
  Project,
  ProjectMember,
  ProjectUpdateRequest,
  TaskItem,
  User,
} from '../types';
import { formatTurkishDate } from '../utils/formatDate';

const emptyUpdateForm: ProjectUpdateRequest = {
  name: '',
  description: '',
  status: '',
  startDate: '',
  dueDate: '',
};

export function ProjectDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const canManageMembers = user?.roles.some(
    (role) => role === 'Admin' || role === 'ProjectManager'
  ) ?? false;

  const [project, setProject] =
    useState<Project | null>(null);

  const [tasks, setTasks] =
    useState<TaskItem[]>([]);

  const [members, setMembers] =
    useState<ProjectMember[]>([]);

  const [availableUsers, setAvailableUsers] =
    useState<User[]>([]);

  const [newMemberUserId, setNewMemberUserId] =
    useState('');

  const [newMemberRole, setNewMemberRole] =
    useState('TeamMember');

  const [loading, setLoading] =
    useState(true);

  const [memberLoading, setMemberLoading] =
    useState(false);

  const [memberError, setMemberError] =
    useState('');

  const [editing, setEditing] =
    useState(false);

  const [editForm, setEditForm] =
    useState<ProjectUpdateRequest>(
      emptyUpdateForm
    );

  const [editLoading, setEditLoading] =
    useState(false);

  const [projectError, setProjectError] =
    useState('');

  const [projectSuccess, setProjectSuccess] =
    useState('');

  const loadData = async () => {
    if (!id) return;

    try {
      setLoading(true);
      setMemberError('');
      setProjectError('');

      const [projectResponse, tasksResponse, membersResponse, availableUsersResponse] = await Promise.all([
        projectApi.getById(id),
        taskApi.getByProject(id),
        projectApi.getMembers(id),
        canManageMembers
          ? projectApi.getAvailableUsers(id)
          : Promise.resolve({ data: [] as User[] }),
      ]);

      setProject(projectResponse.data);
      setTasks(tasksResponse.data);
      setMembers(membersResponse.data);
      setAvailableUsers(
        availableUsersResponse.data
      );
    } catch (error) {
      console.error(error);

      setMemberError(
        'Proje bilgileri yüklenirken bir hata oluştu.'
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, [id, canManageMembers]);

  const handleAddMember = async () => {
    if (!id) return;

    if (!newMemberUserId) {
      setMemberError(
        'Lütfen eklenecek kullanıcıyı seçiniz.'
      );
      return;
    }

    try {
      setMemberLoading(true);
      setMemberError('');

      await projectApi.addMember(id, {
        userId: newMemberUserId,
        role: newMemberRole,
      });

      const [membersResponse, availableUsersResponse] = await Promise.all([
        projectApi.getMembers(id),
        canManageMembers
          ? projectApi.getAvailableUsers(id)
          : Promise.resolve({ data: [] as User[] }),
      ]);

      setMembers(membersResponse.data);
      setAvailableUsers(
        availableUsersResponse.data
      );

      setNewMemberUserId('');
      setNewMemberRole('TeamMember');
    } catch (error) {
      console.error(error);

      setMemberError(
        'Üye eklenirken bir hata oluştu.'
      );
    } finally {
      setMemberLoading(false);
    }
  };

  const handleEditStart = () => {
    if (!project) return;

    setProjectError('');
    setProjectSuccess('');

    setEditForm({
      name: project.name,
      description:
        project.description ?? '',
      status: project.status,
      startDate:
        project.startDate
          ? project.startDate.substring(0, 10)
          : '',
      dueDate:
        project.dueDate
          ? project.dueDate.substring(0, 10)
          : '',
    });

    setEditing(true);
  };

  const handleEditCancel = () => {
    setEditing(false);
    setEditForm(emptyUpdateForm);
    setProjectError('');
  };

  const handleUpdate = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    if (!id) return;

    try {
      setEditLoading(true);
      setProjectError('');
      setProjectSuccess('');

      const response =
        await projectApi.update(
          id,
          {
            name:
              editForm.name?.trim() ||
              undefined,

            description:
              editForm.description?.trim() ||
              null,

            status:
              editForm.status ||
              undefined,

            startDate:
              editForm.startDate ||
              null,

            dueDate:
              editForm.dueDate ||
              null,
          }
        );

      setProject(response.data);

      setEditing(false);
      setEditForm(emptyUpdateForm);

      setProjectSuccess(
        'Proje başarıyla güncellendi.'
      );
    } catch (error) {
      console.error(error);

      setProjectError(
        'Proje güncellenemedi.'
      );
    } finally {
      setEditLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!id || !project) return;

    const confirmed =
      window.confirm(
        `"${project.name}" projesini silmek istediğinize emin misiniz?`
      );

    if (!confirmed) {
      return;
    }

    try {
      setProjectError('');
      setProjectSuccess('');

      await projectApi.remove(id);

      navigate('/projects');
    } catch (error) {
      console.error(error);

      setProjectError(
        'Proje silinemedi.'
      );
    }
  };

  const getUserDisplayName = (
    user: User
  ) => {
    const fullName =
      `${user.firstName ?? ''} ${user.lastName ?? ''}`
        .trim();

    if (fullName) {
      return `${fullName} - ${user.email}`;
    }

    return `${user.userName} - ${user.email}`;
  };

  if (loading) {
    return (
      <div className="page-stack">
        Yükleniyor...
      </div>
    );
  }

  if (!project) {
    return (
      <div className="page-stack">
        Proje bulunamadı.
      </div>
    );
  }

  return (
    <div className="page-stack">
      <section className="panel">
        <div className="panel-header">
          <div>
            <h3>{project.name}</h3>

            <div className="meta-line">
              <span>
                Status: {project.status}
              </span>

              <span>
                Due:{' '}
                {formatTurkishDate(
                  project.dueDate
                )}
              </span>
            </div>
          </div>

          {canManageMembers && <div
            style={{
              display: 'flex',
              gap: '10px',
              flexWrap: 'wrap',
            }}
          >
            <button
              type="button"
              onClick={
                handleEditStart
              }
            >
              Düzenle
            </button>

            <button
              type="button"
              onClick={() =>
                void handleDelete()
              }
            >
              Sil
            </button>
          </div>}
        </div>

        <p>
          {project.description ||
            'Açıklama bulunmuyor.'}
        </p>

        {projectSuccess && (
          <div className="success-box">
            {projectSuccess}
          </div>
        )}

        {projectError && (
          <div className="error-box">
            {projectError}
          </div>
        )}

        {editing && canManageMembers && (
          <form
            onSubmit={handleUpdate}
            className="form-grid"
            style={{
              marginTop: '20px',
            }}
          >
            <h4>Projeyi Düzenle</h4>

            <label>
              Proje adı

              <input
                value={
                  editForm.name ?? ''
                }
                onChange={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    name:
                      event.target.value,
                  }))
                }
                required
              />
            </label>

            <label>
              Açıklama

              <textarea
                value={
                  editForm.description ??
                  ''
                }
                onChange={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    description:
                      event.target.value,
                  }))
                }
              />
            </label>

            <label>
              Durum

              <select
                value={
                  editForm.status ?? ''
                }
                onChange={(event) =>
                  setEditForm((prev) => ({
                    ...prev,
                    status:
                      event.target.value,
                  }))
                }
              >
                <option value="Planning">
                  Planning
                </option>

                <option value="InProgress">
                  InProgress
                </option>

                <option value="Completed">
                  Completed
                </option>

                <option value="Cancelled">
                  Cancelled
                </option>
              </select>
            </label>

            <div className="two-column">
              <label>
                Başlangıç tarihi

                <input
                  type="date"
                  value={
                    editForm.startDate ??
                    ''
                  }
                  onChange={(event) =>
                    setEditForm((prev) => ({
                      ...prev,
                      startDate:
                        event.target.value,
                    }))
                  }
                />
              </label>

              <label>
                Bitiş tarihi

                <input
                  type="date"
                  value={
                    editForm.dueDate ??
                    ''
                  }
                  onChange={(event) =>
                    setEditForm((prev) => ({
                      ...prev,
                      dueDate:
                        event.target.value,
                    }))
                  }
                />
              </label>
            </div>

            <div
              style={{
                display: 'flex',
                gap: '10px',
                flexWrap: 'wrap',
              }}
            >
              <button
                type="submit"
                disabled={editLoading}
              >
                {editLoading
                  ? 'Kaydediliyor...'
                  : 'Değişiklikleri Kaydet'}
              </button>

              <button
                type="button"
                onClick={
                  handleEditCancel
                }
                disabled={editLoading}
              >
                İptal
              </button>
            </div>
          </form>
        )}
      </section>

      <section className="panel">
        <div className="panel-header">
          <h3>Proje Üyeleri</h3>
        </div>

        <div className="card-list">
          {members.length === 0 ? (
            <div className="empty-state">
              Henüz projeye üye eklenmemiş.
            </div>
          ) : (
            members.map((member) => (
              <div
                key={member.id}
                className="mini-card"
              >
                <h4>{member.role}</h4>

                <p>
                  Kullanıcı ID:{' '}
                  {member.userId}
                </p>

                <div className="meta-line">
                  <span>
                    Katılım:{' '}
                    {formatTurkishDate(
                      member.joinedAt
                    )}
                  </span>
                </div>
              </div>
            ))
          )}
        </div>

        {canManageMembers && <div
          style={{
            marginTop: '20px',
          }}
        >
          <h4>Yeni Üye Ekle</h4>

          <div
            style={{
              display: 'flex',
              gap: '10px',
              flexWrap: 'wrap',
              marginTop: '10px',
            }}
          >
            <select
              value={newMemberUserId}
              onChange={(event) =>
                setNewMemberUserId(
                  event.target.value
                )
              }
              disabled={
                memberLoading ||
                availableUsers.length === 0
              }
              style={{
                flex: 1,
                minWidth: '280px',
              }}
            >
              <option value="">
                Kullanıcı seçiniz
              </option>

              {availableUsers.map(
                (user) => (
                  <option
                    key={user.id}
                    value={user.id}
                  >
                    {getUserDisplayName(
                      user
                    )}
                  </option>
                )
              )}
            </select>

            <select
              value={newMemberRole}
              onChange={(event) =>
                setNewMemberRole(
                  event.target.value
                )
              }
              disabled={memberLoading}
            >
              <option value="TeamMember">
                TeamMember
              </option>

              <option value="ProjectManager">
                ProjectManager
              </option>
            </select>

            <button
              type="button"
              onClick={
                handleAddMember
              }
              disabled={
                memberLoading ||
                !newMemberUserId ||
                availableUsers.length === 0
              }
            >
              {memberLoading
                ? 'Ekleniyor...'
                : 'Üye Ekle'}
            </button>
          </div>

          {availableUsers.length === 0 && (
            <p
              style={{
                marginTop: '10px',
              }}
            >
              Eklenebilecek aktif
              kullanıcı bulunmuyor.
            </p>
          )}

          {memberError && (
            <p
              style={{
                marginTop: '10px',
              }}
            >
              {memberError}
            </p>
          )}
        </div>}
      </section>

      <section className="panel">
        <div className="panel-header">
          <h3>Görevler</h3>
        </div>

        <div className="card-list">
          {tasks.length === 0 ? (
            <div className="empty-state">
              Henüz veri bulunmuyor.
            </div>
          ) : (
            tasks.map((task) => (
              <div
                key={task.id}
                className="mini-card"
              >
                <h4>{task.title}</h4>

                <p>
                  {task.description ||
                    'Açıklama yok'}
                </p>

                <div className="meta-line">
                  <span>
                    {task.status}
                  </span>

                  <span>
                    {task.priority}
                  </span>

                  <span>
                    {formatTurkishDate(
                      task.dueDate
                    )}
                  </span>
                </div>
              </div>
            ))
          )}
        </div>
      </section>

      <div>
        <Link to="/projects">
          Projelere dön
        </Link>
      </div>
    </div>
  );
}
