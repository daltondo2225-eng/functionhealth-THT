import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { AuthProvider } from '../auth/AuthContext'
import { TodosPage } from '../pages/TodosPage'
import { server } from '../test/msw-server'
import type { TodoDto } from '../api/todos'

function renderTodos() {
  localStorage.setItem('todo_token', 'fake.jwt')
  localStorage.setItem('todo_user', JSON.stringify({ id: 'u1', email: 'a@example.com' }))
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/todos']}>
        <AuthProvider>
          <Routes>
            <Route path="/todos" element={<TodosPage />} />
            <Route path="/login" element={<div>Login page</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

function todo(overrides: Partial<TodoDto> = {}): TodoDto {
  return {
    id: 't1',
    title: 'Buy milk',
    description: null,
    isCompleted: false,
    createdAt: '2026-05-19T10:00:00Z',
    updatedAt: '2026-05-19T10:00:00Z',
    ...overrides,
  }
}

describe('TodosPage', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('renders list, creates a new todo, and shows it after invalidation', async () => {
    const todos: TodoDto[] = [todo()]
    server.use(
      http.get('/api/todos', () => HttpResponse.json(todos)),
      http.post('/api/todos', async ({ request }) => {
        const body = (await request.json()) as { title: string; description: string | null }
        const created = todo({ id: 't2', title: body.title, description: body.description })
        todos.push(created)
        return HttpResponse.json(created, { status: 201 })
      })
    )

    renderTodos()
    const user = userEvent.setup()

    expect(await screen.findByText('Buy milk')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/new task title/i), 'Walk dog')
    await user.click(screen.getByRole('button', { name: /add task/i }))

    expect(await screen.findByText('Walk dog')).toBeInTheDocument()
  })

  it('toggles complete via checkbox and reflects backend truth', async () => {
    let current = todo()
    server.use(
      http.get('/api/todos', () => HttpResponse.json([current])),
      http.put('/api/todos/:id', async ({ request }) => {
        const body = (await request.json()) as { title: string; description: string | null; isCompleted: boolean }
        current = { ...current, ...body }
        return HttpResponse.json(current)
      })
    )

    renderTodos()
    const user = userEvent.setup()

    const checkbox = await screen.findByRole('checkbox', { name: /mark "buy milk" as complete/i })
    expect(checkbox).not.toBeChecked()

    await user.click(checkbox)

    expect(await screen.findByRole('checkbox', { name: /mark "buy milk" as incomplete/i })).toBeChecked()
  })

  it('preserves the edit draft when the save fails', async () => {
    server.use(
      http.get('/api/todos', () => HttpResponse.json([todo()])),
      http.put('/api/todos/:id', () =>
        HttpResponse.json(
          { error: { code: 'server_error', message: 'Could not save changes.' } },
          { status: 500 }
        )
      )
    )

    renderTodos()
    const user = userEvent.setup()

    await screen.findByText('Buy milk')
    await user.click(screen.getByRole('button', { name: /edit/i }))

    const titleInput = await screen.findByLabelText(/edit title/i)
    await user.clear(titleInput)
    await user.type(titleInput, 'Buy oat milk')
    await user.click(screen.getByRole('button', { name: /save/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not save changes/i)
    expect(screen.getByLabelText(/edit title/i)).toHaveValue('Buy oat milk')
  })

  it('deletes a todo and removes it from the list', async () => {
    let todos: TodoDto[] = [todo(), todo({ id: 't2', title: 'Walk dog' })]
    server.use(
      http.get('/api/todos', () => HttpResponse.json(todos)),
      http.delete('/api/todos/:id', ({ params }) => {
        todos = todos.filter((t) => t.id !== params.id)
        return new HttpResponse(null, { status: 204 })
      })
    )

    renderTodos()
    const user = userEvent.setup()

    const milkRow = (await screen.findByText('Buy milk')).closest('.todo-item') as HTMLElement
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    await user.click(within(milkRow).getByRole('button', { name: /delete/i }))

    await screen.findByText('Walk dog')
    expect(screen.queryByText('Buy milk')).not.toBeInTheDocument()
  })
})
