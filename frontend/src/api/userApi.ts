import { api } from './axios';
import type {
  User,
  UserCreateRequest,
  UserListResponse,
  UserStatusUpdateRequest,
  UserUpdateRequest,
} from '../types';

export const userApi = {
  getAll: () => api.get<UserListResponse[]>('/User'),
  getById: (id: string) => api.get<User>(`/User/${id}`),
  create: (payload: UserCreateRequest) => api.post<User>('/User', payload),
  update: (id: string, payload: UserUpdateRequest) => api.put<User>(`/User/${id}`, payload),
  updateStatus: (id: string, payload: UserStatusUpdateRequest) =>
    api.patch<User>(`/User/${id}/status`, payload),
};
