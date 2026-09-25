import { useEffect, useState } from 'react';
import { notificationApi } from '../api/notificationApi';
import type { NotificationItem } from '../types';

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  useEffect(() => {
    const loadNotifications = async () => {
      try {
        const response = await notificationApi.getAll();
        setNotifications(response.data);
      } catch (error) {
        console.error(error);
      }
    };

    void loadNotifications();
  }, []);

  return (
    <div className="page-stack">
      <section className="panel">
        <div className="panel-header">
          <h3>Bildirimler</h3>
        </div>

        <ul className="list-group">
          {notifications.length === 0 ? (
            <li className="empty-state">Henüz veri bulunmuyor.</li>
          ) : (
            notifications.map((notification) => (
              <li key={notification.id}>
                <strong>{notification.title}</strong>
                <span>{notification.message}</span>
              </li>
            ))
          )}
        </ul>
      </section>
    </div>
  );
}
