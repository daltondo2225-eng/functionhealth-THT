import { api } from './client'

export interface UserDto {
  id: string
  email: string
}

export interface AuthResponse {
  token: string
  user: UserDto
}

export const authApi = {
  register: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/register', { email, password }).then((r) => r.data),
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }).then((r) => r.data),
}
