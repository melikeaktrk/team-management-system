import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';
import { loginSchema, type LoginFormValues } from '../features/auth/loginSchema';

export function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: 'admin@test.com',
      password: 'Admin123!',
    },
  });

  const onSubmit = async (values: LoginFormValues) => {
    try {
      await login(values.email, values.password);
      navigate('/', { replace: true });
    } catch (err: unknown) {
      const message =
        err && typeof err === 'object' && 'response' in err && err.response && typeof err.response === 'object' && 'data' in err.response
          ? (err.response as { data?: { message?: string } }).data?.message
          : 'Giriş başarısız oldu.';

      setError('root', { message: message || 'Giriş e-posta veya şifre hatalı.' });
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        {/* Logo / Marka Alanı */}
        <div className="auth-brand">
          <div className="brand-logo">TM</div>
          <h2>TeamTask Manager</h2>
        </div>

        <div className="auth-header">
          <h3>Hoş geldiniz</h3>
          <p>Devam etmek için hesabınıza giriş yapın.</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="form-grid" noValidate>
          {/* E-posta Alanı */}
          <label>
            E-posta
            <input 
              type="email" 
              placeholder="ornek@email.com"
              {...register('email')} 
            />
            {errors.email && <span className="field-error">{errors.email.message}</span>}
          </label>

          {/* Şifre Alanı + Göster/Gizle */}
          <label className="password-field">
            <span>Şifre</span>
            <div className="password-input-wrapper">
              <input
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                {...register('password')}
              />
              <button
                type="button"
                className="toggle-password"
                onClick={() => setShowPassword((prev) => !prev)}
                tabIndex={-1}
              >
                {showPassword ? '🙈' : '👁️'}
              </button>
            </div>
            {errors.password && <span className="field-error">{errors.password.message}</span>}
          </label>

          {/* Hata Kutusu */}
          {errors.root && <div className="error-box">{errors.root.message}</div>}

          {/* Giriş Yap Butonu */}
          <button type="submit" disabled={isSubmitting} className="btn-primary">
            {isSubmitting ? 'Giriş yapılıyor...' : 'Giriş yap'}
          </button>
        </form>
      </div>
    </div>
  );
}