import { api } from './client';
import type {
  TaskCommentCreateRequest,
  TaskItem,
  TaskItemCreateRequest,
  TaskItemUpdateRequest,
} from '../types';

export const taskApi = {
  getByProject: (projectId: string) =>
    api.get<TaskItem[]>(`/api/Task/project/${projectId}`),
  getById: (id: string) => api.get<TaskItem>(`/api/Task/${id}`),
  create: (payload: TaskItemCreateRequest) => api.post<TaskItem>('/api/Task', payload),
  update: (id: string, payload: TaskItemUpdateRequest) =>
    api.put<TaskItem>(`/api/Task/${id}`, payload),
  remove: (id: string) => api.delete(`/api/Task/${id}`),
  addComment: (taskId: string, payload: TaskCommentCreateRequest) =>
    api.post(`/api/Task/${taskId}/comments`, payload),
};
