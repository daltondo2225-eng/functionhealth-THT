import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import { TodoForm } from '../components/TodoForm'
import { TodoList } from '../components/TodoList'
import { todosApi } from '../api/todos'

export type TodoFilter = 'all' | 'active' | 'completed'

export function TodosPage() {
  const { user, signOut } = useAuth()
  const [filter, setFilter] = useState<TodoFilter>('all')
  const { data } = useQuery({ queryKey: ['todos'], queryFn: todosApi.list })

  const total = data?.length ?? 0
  const completed = data?.filter((t) => t.isCompleted).length ?? 0

  return (
    <div className="app">
      <header className="topbar">
        <h1>Tasks</h1>
        <div className="row">
          <span className="muted">{user?.email}</span>
          <button type="button" className="secondary" onClick={signOut}>
            Sign out
          </button>
        </div>
      </header>
      <TodoForm />
      <div className="row" style={{ marginBottom: 12 }}>
        <span className="muted" aria-live="polite">
          {completed} of {total} done
        </span>
        <div className="spacer" />
        <div className="row" role="group" aria-label="Filter tasks">
          {(['all', 'active', 'completed'] as TodoFilter[]).map((value) => (
            <button
              key={value}
              type="button"
              className={filter === value ? '' : 'secondary'}
              aria-pressed={filter === value}
              onClick={() => setFilter(value)}
              style={{ textTransform: 'capitalize' }}
            >
              {value}
            </button>
          ))}
        </div>
      </div>
      <TodoList filter={filter} />
    </div>
  )
}
