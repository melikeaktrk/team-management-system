import { useAuth } from '../features/auth/AuthContext';

export function ProfilePage() {
  const { user } = useAuth();

  return (
    <div className="page-stack">
      <section className="panel">
        <div className="panel-header">
          <h3>Profil</h3>
        </div>

        <div className="profile-grid">
          <div className="profile-card">
            <h4>Kullanıcı adı</h4>
            <p>{user?.userName || 'Belirtilmemiş'}</p>
          </div>
          <div className="profile-card">
            <h4>E-posta</h4>
            <p>{user?.email || 'Belirtilmemiş'}</p>
          </div>
          <div className="profile-card">
            <h4>Roller</h4>
            <p>{user?.roles?.length ? user.roles.join(', ') : 'Yok'}</p>
          </div>
        </div>
      </section>
    </div>
  );
}
