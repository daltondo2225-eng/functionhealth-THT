import axios, { AxiosError } from 'axios'

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]>
}

const TOKEN_KEY = 'todo_token'

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t: string) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let onUnauthorized: (() => void) | null = null
export const setUnauthorizedHandler = (fn: () => void) => {
  onUnauthorized = fn
}

api.interceptors.response.use(
  (r) => r,
  (error: AxiosError<{ error?: ApiError }>) => {
    if (error.response?.status === 401) {
      tokenStorage.clear()
      onUnauthorized?.()
    }
    return Promise.reject(normalizeError(error))
  }
)

export function normalizeError(error: unknown): ApiError {
  if (axios.isAxiosError(error)) {
    const body = error.response?.data as { error?: ApiError } | undefined
    if (body?.error) return body.error
    if (error.response) {
      return {
        code: 'http_error',
        message: `Request failed (${error.response.status})`,
      }
    }
    return { code: 'network_error', message: 'Network error. Check your connection.' }
  }
  return { code: 'unknown_error', message: 'Something went wrong.' }
}
