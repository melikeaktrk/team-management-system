import { api } from './client';
import type {
  TaskCommentCreateRequest,
  TaskAttachment,
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
  getComments: (taskId: string) =>
    api.get<{ id: string; taskItemId: string; userId: string; content: string; createdAt: string }[]>(
      `/api/Task/${taskId}/comments`
    ),
  getAttachments: (taskId: string) =>
    api.get<TaskAttachment[]>(`/api/Task/${taskId}/attachments`),
  uploadAttachment: (taskId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post<TaskAttachment>(`/api/Task/${taskId}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  downloadAttachment: (attachmentId: string) =>
    api.get<Blob>(`/api/Task/attachments/${attachmentId}`, { responseType: 'blob' }),
};
