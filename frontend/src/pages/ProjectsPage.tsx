import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ConfirmationModal } from '../components/ConfirmationModal';
import { projectApi } from '../api/projectApi';
import { useAuth } from '../features/auth/AuthContext';
import type {
  Project,
  ProjectCreateRequest,
  ProjectUpdateRequest,
} from '../types';
import { formatTurkishDate } from '../utils/formatDate';

const emptyForm: ProjectCreateRequest = {
  name: '',
  description: '',
  startDate: '',
  dueDate: '',
};

const emptyUpdateForm: ProjectUpdateRequest = {
  name: '',
  description: '',
  status: '',
  startDate: '',
  dueDate: '',
};

export function ProjectsPage() {
  const { user } = useAuth();
  const canManageProjects = user?.roles.some(
    (role) => role === 'Admin' || role === 'ProjectManager'
  ) ?? false;
  const [projects, setProjects] = useState<Project[]>([]);

  const [form, setForm] =
    useState<ProjectCreateRequest>(emptyForm);

  const [editingProjectId, setEditingProjectId] =
    useState<string | null>(null);

  const [editForm, setEditForm] =
    useState<ProjectUpdateRequest>(emptyUpdateForm);

  const [error, setError] =
    useState<string | null>(null);

  const [success, setSuccess] =
    useState<string | null>(null);

  const [loading, setLoading] =
    useState(false);

  const [deletingProjectId, setDeletingProjectId] =
    useState<string | null>(null);

  const [projectToDelete, setProjectToDelete] =
    useState<Project | null>(null);

  const [deleteError, setDeleteError] =
    useState<string | null>(null);

  const loadProjects = async () => {
    try {
      setError(null);

      const response =
        await projectApi.getAll();

      setProjects(response.data);
    } catch (err) {
      console.error(err);

      setError(
        'Projeler yüklenemedi.'
      );
    }
  };

  useEffect(() => {
    void loadProjects();
  }, []);

  const handleCreate = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    try {
      setLoading(true);
      setError(null);
      setSuccess(null);

      await projectApi.create({
        ...form,
        description:
          form.description || undefined,
        startDate:
          form.startDate || null,
        dueDate:
          form.dueDate || null,
      });

      setForm(emptyForm);

      await loadProjects();

      setSuccess(
        'Proje başarıyla oluşturuldu.'
      );
    } catch (err) {
      console.error(err);

      setError(
        'Proje oluşturulamadı.'
      );
    } finally {
      setLoading(false);
    }
  };

  const handleEditStart = (
    project: Project
  ) => {
    setError(null);
    setSuccess(null);

    setEditingProjectId(project.id);

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
  };

  const handleEditCancel = () => {
    setEditingProjectId(null);
    setEditForm(emptyUpdateForm);
    setError(null);
  };

  const handleUpdate = async (
    event: React.FormEvent
  ) => {
    event.preventDefault();

    if (!editingProjectId) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setSuccess(null);

      await projectApi.update(
        editingProjectId,
        {
          name:
            editForm.name?.trim() || undefined,

          description:
            editForm.description?.trim() || null,

          status:
            editForm.status || undefined,

          startDate:
            editForm.startDate || null,

          dueDate:
            editForm.dueDate || null,
        }
      );

      setEditingProjectId(null);
      setEditForm(emptyUpdateForm);

      await loadProjects();

      setSuccess(
        'Proje başarıyla güncellendi.'
      );
    } catch (err) {
      console.error(err);

      setError(
        'Proje güncellenemedi.'
      );
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!projectToDelete) return;
    const project = projectToDelete;

    try {
      setDeletingProjectId(project.id);
      setError(null);
      setSuccess(null);
      setDeleteError(null);

      await projectApi.remove(project.id);

      setProjects((currentProjects) =>
        currentProjects.filter(
          (item) =>
            item.id !== project.id
        )
      );

      setSuccess(
        'Proje başarıyla silindi.'
      );
      setProjectToDelete(null);
    } catch (err) {
      console.error(err);
      setDeleteError('Proje silinemedi. Lütfen tekrar deneyin.');
    } finally {
      setDeletingProjectId(null);
    }
  };

  return (
    <div className="page-stack">
      {canManageProjects && (
      <section className="panel">
        <div className="panel-header">
          <h3>Yeni proje</h3>
        </div>

        <form
          onSubmit={handleCreate}
          className="form-grid"
        >
          <label>
            Proje adı

            <input
              value={form.name}
              onChange={(event) =>
                setForm((prev) => ({
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
                form.description
              }
              onChange={(event) =>
                setForm((prev) => ({
                  ...prev,
                  description:
                    event.target.value,
                }))
              }
            />
          </label>

          <div className="two-column">
            <label>
              Başlangıç tarihi

              <input
                type="date"
                value={
                  form.startDate ?? ''
                }
                onChange={(event) =>
                  setForm((prev) => ({
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
                  form.dueDate ?? ''
                }
                onChange={(event) =>
                  setForm((prev) => ({
                    ...prev,
                    dueDate:
                      event.target.value,
                  }))
                }
              />
            </label>
          </div>

          {error && (
            <div className="error-box">
              {error}
            </div>
          )}

          {success && (
            <div className="success-box">
              {success}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
          >
            {loading
              ? 'Kaydediliyor...'
              : 'Proje ekle'}
          </button>
        </form>
      </section>
      )}

      <section className="panel">
        <div className="panel-header">
          <h3>Projeler</h3>
        </div>

        <div className="card-list">
          {projects.length === 0 ? (
            <div className="empty-state">
              Henüz veri bulunmuyor.
            </div>
          ) : (
            projects.map((project) => {
              const isEditing = canManageProjects &&
                editingProjectId ===
                project.id;

              return (
                <div
                  key={project.id}
                  className="mini-card wide"
                >
                  {!isEditing ? (
                    <>
                      <h4>
                        {project.name}
                      </h4>

                      <p>
                        {project.description ||
                          'Açıklama yok.'}
                      </p>

                      <div className="meta-line">
                        <span>
                          {project.status}
                        </span>

                        <span>
                          {formatTurkishDate(
                            project.dueDate
                          )}
                        </span>
                      </div>

                      <div
                        style={{
                          display: 'flex',
                          gap: '10px',
                          flexWrap:
                            'wrap',
                          marginTop:
                            '15px',
                        }}
                      >
                        <Link
                          to={`/projects/${project.id}`}
                        >
                          <button
                            type="button"
                          >
                            Detay
                          </button>
                        </Link>

                        {canManageProjects && <button
                          type="button"
                          onClick={() =>
                            handleEditStart(
                              project
                            )
                          }
                        >
                          Düzenle
                        </button>}

                        {canManageProjects && <button
                          type="button"
                          onClick={() => {
                            setDeleteError(null);
                            setProjectToDelete(project);
                          }}
                          disabled={
                            deletingProjectId ===
                            project.id
                          }
                        >
                          {deletingProjectId ===
                          project.id
                            ? 'Siliniyor...'
                            : 'Sil'}
                        </button>}
                      </div>
                    </>
                  ) : (
                    <form
                      onSubmit={
                        handleUpdate
                      }
                      className="form-grid"
                    >
                      <h4>
                        Projeyi Düzenle
                      </h4>

                      <label>
                        Proje adı

                        <input
                          value={
                            editForm.name ??
                            ''
                          }
                          onChange={(
                            event
                          ) =>
                            setEditForm(
                              (prev) => ({
                                ...prev,
                                name:
                                  event
                                    .target
                                    .value,
                              })
                            )
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
                          onChange={(
                            event
                          ) =>
                            setEditForm(
                              (prev) => ({
                                ...prev,
                                description:
                                  event
                                    .target
                                    .value,
                              })
                            )
                          }
                        />
                      </label>

                      <label>
                        Durum

                        <select
                          value={
                            editForm.status ??
                            ''
                          }
                          onChange={(
                            event
                          ) =>
                            setEditForm(
                              (prev) => ({
                                ...prev,
                                status:
                                  event
                                    .target
                                    .value,
                              })
                            )
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
                            onChange={(
                              event
                            ) =>
                              setEditForm(
                                (prev) => ({
                                  ...prev,
                                  startDate:
                                    event
                                      .target
                                      .value,
                                })
                              )
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
                            onChange={(
                              event
                            ) =>
                              setEditForm(
                                (prev) => ({
                                  ...prev,
                                  dueDate:
                                    event
                                      .target
                                      .value,
                                })
                              )
                            }
                          />
                        </label>
                      </div>

                      <div
                        style={{
                          display: 'flex',
                          gap: '10px',
                          flexWrap:
                            'wrap',
                        }}
                      >
                        <button
                          type="submit"
                          disabled={loading}
                        >
                          {loading
                            ? 'Kaydediliyor...'
                            : 'Değişiklikleri Kaydet'}
                        </button>

                        <button
                          type="button"
                          onClick={
                            handleEditCancel
                          }
                          disabled={loading}
                        >
                          İptal
                        </button>
                      </div>
                    </form>
                  )}
                </div>
              );
            })
          )}
        </div>
      </section>
      <ConfirmationModal
        isOpen={projectToDelete !== null}
        projectName={projectToDelete?.name ?? ''}
        isLoading={deletingProjectId !== null}
        error={deleteError}
        onConfirm={() => void handleDelete()}
        onCancel={() => {
          if (deletingProjectId !== null) return;
          setProjectToDelete(null);
          setDeleteError(null);
        }}
      />
    </div>
  );
}
