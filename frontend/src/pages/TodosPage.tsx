import { useAuth } from '../auth/AuthContext'
import { TodoForm } from '../components/TodoForm'
import { TodoList } from '../components/TodoList'

export function TodosPage() {
  const { user, signOut } = useAuth()
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
      <TodoList />
    </div>
  )
}
