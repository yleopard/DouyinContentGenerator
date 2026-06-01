import api from './api';
import type { LoginRequest, RegisterRequest, AuthResponse, UserInfo } from '../types';

export const authService = {
  login: (data: LoginRequest) => api.post<AuthResponse>('/auth/login', data),
  register: (data: RegisterRequest) => api.post<AuthResponse>('/auth/register', data),
  getMe: () => api.get<UserInfo>('/auth/me'),
};
