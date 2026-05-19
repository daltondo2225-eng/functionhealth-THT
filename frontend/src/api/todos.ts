import { api } from './client'

export interface TodoDto {
  id: string
  title: string
  description: string | null
  isCompleted: boolean
  createdAt: string
  updatedAt: string
}

export interface TodoCreate {
  title: string
  description?: string | null
}

export interface TodoUpdate {
  title: string
  description?: string | null
  isCompleted: boolean
}

export const todosApi = {
  list: () => api.get<TodoDto[]>('/todos').then((r) => r.data),
  create: (input: TodoCreate) => api.post<TodoDto>('/todos', input).then((r) => r.data),
  update: (id: string, input: TodoUpdate) =>
    api.put<TodoDto>(`/todos/${id}`, input).then((r) => r.data),
  remove: (id: string) => api.delete(`/todos/${id}`).then(() => undefined),
}
