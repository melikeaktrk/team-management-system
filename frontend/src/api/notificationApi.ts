import { api } from './client';
import type { NotificationItem } from '../types';

export const notificationApi = {
  getAll: () => api.get<NotificationItem[]>('/api/Notification'),
  markAsRead: (id: string) => api.patch<NotificationItem>(`/api/Notification/${id}/read`),
  markAllAsRead: () => api.patch<{ updatedCount: number }>('/api/Notification/read-all'),
};
