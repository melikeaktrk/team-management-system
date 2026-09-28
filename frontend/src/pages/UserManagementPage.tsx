import { useEffect, useState } from 'react';
import { userApi } from '../api/userApi';
import type { User, UserUpdateRequest } from '../types';

const emptyForm = {
  userName: '',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  role: 'TeamMember',
};

export function UserManagementPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<UserUpdateRequest>({});

  const loadUsers = async () => {
    try {
      const response = await userApi.getAll();
      setUsers(response.data);
    } catch (err) {
      console.error(err);
      setError('Kullanıcılar yüklenemedi.');
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      // POST /api/User çağrısı
      await userApi.create(form);
      setForm(emptyForm); // Formu sıfırla
      await loadUsers();  // Kullanıcı listesini otomatik güncelle
      setSuccess('Kullanıcı oluşturuldu.');
    } catch (err) {
      console.error(err);
      setError('Kullanıcı oluşturulamadı. Bilgileri kontrol ediniz.');
    }
  };

  const toggleStatus = async (user: User) => {
    try {
      await userApi.updateStatus(user.id, { isActive: !user.isActive });
      await loadUsers();
    } catch (err) {
      console.error(err);
      setError('Durum güncellenemedi.');
    }
  };

  const updateRole = async (user: User, role: string) => {
    try {
      await userApi.updateRole(user.id, role);
      await loadUsers();
    } catch (err) {
      console.error(err);
      setError('Kullanıcı rolü güncellenemedi.');
    }
  };

  const startEdit = (user: User) => {
    setError(null);
    setSuccess(null);
    setEditingUserId(user.id);
    setEditForm({ userName: user.userName, email: user.email, firstName: user.firstName ?? '', lastName: user.lastName ?? '' });
  };

  const saveUser = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editingUserId) return;
    setError(null);
    setSuccess(null);
    try {
      await userApi.update(editingUserId, editForm);
      setEditingUserId(null);
      setEditForm({});
      await loadUsers();
      setSuccess('Kullanıcı bilgileri güncellendi.');
    } catch (err) {
      console.error(err);
      setError('Kullanıcı bilgileri güncellenemedi. E-posta veya kullanıcı adı kullanımda olabilir.');
    }
  };

  return (
    <div className="page-stack">
      {error && <div className="error-box">{error}</div>}
      {success && <div className="success-box">{success}</div>}
      {editingUserId && <section className="panel">
        <div className="panel-header"><h3>Kullanıcı bilgilerini düzenle</h3></div>
        <form onSubmit={saveUser} className="form-grid">
          <div className="two-column">
            <label>Kullanıcı adı<input required maxLength={100} value={editForm.userName ?? ''} onChange={(event) => setEditForm((current) => ({ ...current, userName: event.target.value }))} /></label>
            <label>E-posta<input required type="email" maxLength={255} value={editForm.email ?? ''} onChange={(event) => setEditForm((current) => ({ ...current, email: event.target.value }))} /></label>
          </div>
          <div className="two-column">
            <label>Ad<input maxLength={100} value={editForm.firstName ?? ''} onChange={(event) => setEditForm((current) => ({ ...current, firstName: event.target.value }))} /></label>
            <label>Soyad<input maxLength={100} value={editForm.lastName ?? ''} onChange={(event) => setEditForm((current) => ({ ...current, lastName: event.target.value }))} /></label>
          </div>
          <div className="filter-actions"><button type="submit">Değişiklikleri kaydet</button><button type="button" onClick={() => setEditingUserId(null)}>Vazgeç</button></div>
        </form>
      </section>}
      {/* 1. ÜST KISIM: Yeni Kullanıcı Oluştur Formu */}
      <section className="panel">
        <div className="panel-header">
          <h3>Yeni kullanıcı oluştur</h3>
        </div>
        <form onSubmit={handleSubmit} className="form-grid">
          <div className="two-column">
            <label>
              Kullanıcı adı
              <input
                type="text"
                value={form.userName}
                onChange={(e) => setForm((prev) => ({ ...prev, userName: e.target.value }))}
                required
              />
            </label>
            <label>
              E-posta
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm((prev) => ({ ...prev, email: e.target.value }))}
                required
              />
            </label>
          </div>

          <div className="two-column">
            <label>
              Ad
              <input
                type="text"
                value={form.firstName}
                onChange={(e) => setForm((prev) => ({ ...prev, firstName: e.target.value }))}
              />
            </label>
            <label>
              Soyad
              <input
                type="text"
                value={form.lastName}
                onChange={(e) => setForm((prev) => ({ ...prev, lastName: e.target.value }))}
              />
            </label>
          </div>

          <label>
            Şifre
            <input
              type="password"
              value={form.password}
              onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
              required
            />
          </label>

          <label>
            Rol
            <select
              value={form.role}
              onChange={(e) => setForm((prev) => ({ ...prev, role: e.target.value }))}
            >
              <option value="TeamMember">TeamMember</option>
              <option value="ProjectManager">ProjectManager</option>
              <option value="Admin">Admin</option>
            </select>
          </label>

          <button type="submit">Kullanıcı oluştur</button>
        </form>
      </section>

      {/* 2. ALT KISIM: Mevcut Kullanıcı Listesi */}
      <section className="panel">
        <div className="panel-header">
          <h3>Kullanıcı yönetimi</h3>
        </div>

        <div className="user-table">
          {users.map((user) => (
            <div className="user-row" key={user.id}>
              <div>
                <strong>{user.userName}</strong>
                <span>{user.email}</span>
              </div>
              <div>
                <span>{user.firstName || 'Ad'} {user.lastName || 'Soyad'}</span>
              </div>
              <div>
                <select
                  aria-label={`${user.userName} rolü`}
                  value={user.roles?.[0] ?? 'TeamMember'}
                  onChange={(e) => void updateRole(user, e.target.value)}
                >
                  <option value="TeamMember">TeamMember</option>
                  <option value="ProjectManager">ProjectManager</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div>
                <span className={`status-chip ${user.isActive ? '' : 'inactive'}`}>
                  {user.isActive ? 'Aktif' : 'Pasif'}
                </span>
              </div>
              <div>
                <button type="button" onClick={() => startEdit(user)}>Düzenle</button>
              </div>
              <div>
                <button type="button" onClick={() => toggleStatus(user)}>
                  {user.isActive ? 'Devre Dışı' : 'Aktif Et'}
                </button>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
