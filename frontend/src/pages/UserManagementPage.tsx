import { useEffect, useState } from 'react';
import { userApi } from '../api/userApi';
import type { User } from '../types';

const emptyForm = {
  userName: '',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
};

export function UserManagementPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);

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

    try {
      // POST /api/User çağrısı
      await userApi.create(form);
      setForm(emptyForm); // Formu sıfırla
      await loadUsers();  // Kullanıcı listesini otomatik güncelle
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

  return (
    <div className="page-stack">
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

          {error && <div className="error-box">{error}</div>}

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
                <span className={`status-chip ${user.isActive ? '' : 'inactive'}`}>
                  {user.isActive ? 'Aktif' : 'Pasif'}
                </span>
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