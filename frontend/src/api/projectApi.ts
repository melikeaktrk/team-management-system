
import { api } from './client';
import type {
  Project,
  ProjectCreateRequest,
  ProjectMember,
  ProjectMemberRequest,
  ProjectUpdateRequest,
  User,
  ProjectReport,
} from '../types';

export const projectApi = {
  getAll: () =>
    api.get<Project[]>('/api/Project'),

  getById: (id: string) =>
    api.get<Project>(`/api/Project/${id}`),
  getReport: (id: string) => api.get<ProjectReport>(`/api/Project/${id}/report`),

  create: (payload: ProjectCreateRequest) =>
    api.post<Project>('/api/Project', payload),

  update: (
    id: string,
    payload: ProjectUpdateRequest
  ) =>
    api.put<Project>(
      `/api/Project/${id}`,
      payload
    ),

  remove: (id: string) =>
    api.delete(`/api/Project/${id}`),

  getMembers: (projectId: string) =>
    api.get<ProjectMember[]>(
      `/api/Project/${projectId}/members`
    ),

  getAvailableUsers: (projectId: string) =>
    api.get<User[]>(
      `/api/Project/${projectId}/available-users`
    ),

  addMember: (
    projectId: string,
    payload: ProjectMemberRequest
  ) =>
    api.post(
      `/api/Project/${projectId}/members`,
      payload
    ),

  removeMember: (projectId: string, userId: string) =>
    api.delete(`/api/Project/${projectId}/members/${userId}`),
};

