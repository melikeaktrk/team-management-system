import { api } from './client';
import type { NotificationItem } from '../types';

export const notificationApi = {
  getAll: () => api.get<NotificationItem[]>('/api/Notification'),
};
