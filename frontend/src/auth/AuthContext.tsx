import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { setUnauthorizedHandler, tokenStorage } from '../api/client'
import type { UserDto } from '../api/auth'

interface AuthState {
  token: string | null
  user: UserDto | null
  signIn: (token: string, user: UserDto) => void
  signOut: () => void
}

const AuthContext = createContext<AuthState | null>(null)

const USER_KEY = 'todo_user'

function readUser(): UserDto | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as UserDto) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [token, setToken] = useState<string | null>(tokenStorage.get())
  const [user, setUser] = useState<UserDto | null>(readUser())

  const signIn = useCallback((newToken: string, newUser: UserDto) => {
    tokenStorage.set(newToken)
    localStorage.setItem(USER_KEY, JSON.stringify(newUser))
    setToken(newToken)
    setUser(newUser)
  }, [])

  const signOut = useCallback(() => {
    tokenStorage.clear()
    localStorage.removeItem(USER_KEY)
    setToken(null)
    setUser(null)
    navigate('/login', { replace: true })
  }, [navigate])

  useEffect(() => {
    setUnauthorizedHandler(() => {
      localStorage.removeItem(USER_KEY)
      setToken(null)
      setUser(null)
      navigate('/login', { replace: true })
    })
  }, [navigate])

  const value = useMemo(() => ({ token, user, signIn, signOut }), [token, user, signIn, signOut])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
