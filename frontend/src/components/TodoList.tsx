import { useQuery } from '@tanstack/react-query'
import { todosApi } from '../api/todos'
import { ErrorBanner } from './ErrorBanner'
import { TodoItem } from './TodoItem'
import type { ApiError } from '../api/client'
import type { TodoFilter } from '../pages/TodosPage'

export function TodoList({ filter = 'all' }: { filter?: TodoFilter }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ['todos'],
    queryFn: todosApi.list,
  })

  if (isLoading) {
    return <p className="muted">Loading…</p>
  }

  if (error) {
    return <ErrorBanner message={(error as unknown as ApiError).message} />
  }

  if (!data || data.length === 0) {
    return <p className="empty">No tasks yet. Add one above.</p>
  }

  const visible = data.filter((t) =>
    filter === 'all' ? true : filter === 'completed' ? t.isCompleted : !t.isCompleted
  )

  if (visible.length === 0) {
    return (
      <p className="empty">
        {filter === 'completed' ? 'No completed tasks.' : 'Nothing active. Nice work.'}
      </p>
    )
  }

  return (
    <div className="todo-list">
      {visible.map((todo) => (
        <TodoItem key={todo.id} todo={todo} />
      ))}
    </div>
  )
}
