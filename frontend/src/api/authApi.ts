import { api } from './axios';
import type { AuthMeResponse, AuthResponse, LoginRequest, RegisterRequest } from '../types';

export const authApi = {
  register: (payload: RegisterRequest) =>
    api.post<AuthResponse>('/Auth/register', payload),
  login: (payload: LoginRequest) =>
    api.post<AuthResponse>('/Auth/login', payload),
  me: () => api.get<AuthMeResponse>('/Auth/me'),
};
