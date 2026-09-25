import axios from 'axios';

export const TOKEN_STORAGE_KEY = 'token';

export const api = axios.create({
  baseURL: 'http://localhost:5048/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);

  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      window.location.href = '/login';
    }

    return Promise.reject(error);
  },
);
