import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../features/auth/AuthContext';

export function Layout() {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const isAdmin = user?.roles?.includes('Admin') ?? false;
  const navItems = [
    { to: '/', label: 'Panel' },
    { to: '/projects', label: 'Projeler' },
    { to: '/tasks', label: 'Görevler' },
    { to: '/notifications', label: 'Bildirimler' },
    { to: '/profile', label: 'Profil' },
    ...(isAdmin ? [{ to: '/users', label: 'Kullanıcılar' }] : []),
  ];

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <div className="brand-mark">TM</div>
          <div>
            <h2>Team Manager</h2>
          </div>
        </div>

        <nav className="side-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                ['nav-link', isActive ? 'active' : ''].join(' ')
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <button className="logout-button" onClick={handleLogout} type="button">
          Çıkış yap
        </button>
      </aside>

      <main className="main-panel">
        <header className="topbar">
          <div>
            <p className="eyebrow">Corporate workspace</p>
            <h1 className="app-title">Team task management</h1>
          </div>
          <div className="user-pill">
            {user?.userName || user?.email || 'User'}
          </div>
        </header>

        <Outlet />
      </main>
    </div>
  );
}
