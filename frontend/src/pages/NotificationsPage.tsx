import { useCallback, useEffect, useState } from 'react';
import { notificationApi } from '../api/notificationApi';
import type { NotificationItem } from '../types';
import { formatTurkishDate } from '../utils/formatDate';

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadNotifications = useCallback(async () => {
    try {
      setError('');
      setNotifications((await notificationApi.getAll()).data);
    } catch (requestError) {
      console.error(requestError);
      setError('Bildirimler yüklenemedi.');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void loadNotifications(); }, [loadNotifications]);

  const markOneRead = async (notification: NotificationItem) => {
    try {
      await notificationApi.markAsRead(notification.id);
      setNotifications((current) => current.map((item) => item.id === notification.id ? { ...item, isRead: true } : item));
    } catch (requestError) { console.error(requestError); setError('Bildirim okundu olarak işaretlenemedi.'); }
  };

  const markAllRead = async () => {
    try {
      await notificationApi.markAllAsRead();
      setNotifications((current) => current.map((item) => ({ ...item, isRead: true })));
    } catch (requestError) { console.error(requestError); setError('Bildirimler okundu olarak işaretlenemedi.'); }
  };

  const unreadCount = notifications.filter((notification) => !notification.isRead).length;

  return (
    <div className="page-stack">
      <section className="panel">
        <div className="panel-header">
          <div><h3>Bildirimler</h3><span>{unreadCount} okunmamış</span></div>
          <button type="button" onClick={() => void markAllRead()} disabled={unreadCount === 0}>Tümünü okundu işaretle</button>
        </div>
        {error && <div className="error-box">{error}</div>}
        {loading ? <div className="loading-state">Bildirimler yükleniyor...</div> : (
          <ul className="list-group">
            {notifications.length === 0 ? <li className="empty-state">Henüz bildiriminiz yok.</li> : notifications.map((notification) => (
              <li key={notification.id} className={notification.isRead ? 'notification-read' : 'notification-unread'}>
                <div className="notification-content"><strong>{notification.title}</strong><span>{notification.message}</span>
                  <small>{formatTurkishDate(notification.createdAt)} · {notification.type}</small>
                </div>
                {!notification.isRead && <button type="button" onClick={() => void markOneRead(notification)}>Okundu işaretle</button>}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
