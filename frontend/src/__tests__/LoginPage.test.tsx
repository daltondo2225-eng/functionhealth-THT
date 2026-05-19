import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { http, HttpResponse } from 'msw'
import { AuthProvider } from '../auth/AuthContext'
import { LoginPage } from '../pages/LoginPage'
import { server } from '../test/msw-server'

function renderLogin() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/login']}>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/todos" element={<div>Tasks page</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('logs in successfully and navigates to /todos', async () => {
    server.use(
      http.post('/api/auth/login', async () => {
        return HttpResponse.json({
          token: 'fake.jwt.token',
          user: { id: 'u1', email: 'a@example.com' },
        })
      })
    )

    renderLogin()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/email/i), 'a@example.com')
    await user.type(screen.getByLabelText(/password/i), 'password123')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText(/tasks page/i)).toBeInTheDocument()
    expect(localStorage.getItem('todo_token')).toBe('fake.jwt.token')
  })

  it('shows error on 401 and preserves typed email', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json(
          { error: { code: 'invalid_credentials', message: 'Invalid email or password.' } },
          { status: 401 }
        )
      )
    )

    renderLogin()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/email/i), 'a@example.com')
    await user.type(screen.getByLabelText(/password/i), 'wrongpass')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/invalid email or password/i)
    expect(screen.getByLabelText(/email/i)).toHaveValue('a@example.com')
  })
})
