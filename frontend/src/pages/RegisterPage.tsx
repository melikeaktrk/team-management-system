import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuth } from '../features/auth/AuthContext';
import type { RegisterRequest } from '../types';

const initialValues: RegisterRequest = {
  userName: '',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
};

export function RegisterPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [form, setForm] = useState<RegisterRequest>(initialValues);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleChange = (field: keyof RegisterRequest, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await authApi.register(form);
      await login(form.email, form.password);
      navigate('/');
    } catch (err: unknown) {
      const message =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : 'Kayıt işlemi başarısız oldu.';
      setError(message || 'Kayıt işlemi başarısız oldu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h2>Hesap oluştur</h2>
        <p>Takımınız için yeni bir hesap açın.</p>

        <form onSubmit={handleSubmit} className="form-grid">
          <div className="two-column">
            <label>
              Ad
              <input
                value={form.firstName}
                onChange={(e) => handleChange('firstName', e.target.value)}
              />
            </label>
            <label>
              Soyad
              <input
                value={form.lastName}
                onChange={(e) => handleChange('lastName', e.target.value)}
              />
            </label>
          </div>

          <label>
            Kullanıcı adı
            <input
              value={form.userName}
              onChange={(e) => handleChange('userName', e.target.value)}
              required
            />
          </label>

          <label>
            E-posta
            <input
              type="email"
              value={form.email}
              onChange={(e) => handleChange('email', e.target.value)}
              required
            />
          </label>

          <label>
            Şifre
            <input
              type="password"
              value={form.password}
              onChange={(e) => handleChange('password', e.target.value)}
              required
            />
          </label>

          {error && <div className="error-box">{error}</div>}

          <button type="submit" disabled={loading}>
            {loading ? 'Kaydediliyor...' : 'Kayıt ol'}
          </button>
        </form>

        <div className="auth-footer">
          Zaten hesabınız var mı? <Link to="/login">Giriş yap</Link>
        </div>
      </div>
    </div>
  );
}
