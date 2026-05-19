import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { todosApi, type TodoDto } from '../api/todos'
import type { ApiError } from '../api/client'

export function TodoItem({ todo }: { todo: TodoDto }) {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [draftTitle, setDraftTitle] = useState(todo.title)
  const [draftDescription, setDraftDescription] = useState(todo.description ?? '')
  const [error, setError] = useState<string | null>(null)

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['todos'] })

  const updateMutation = useMutation({
    mutationFn: (input: { title: string; description: string | null; isCompleted: boolean }) =>
      todosApi.update(todo.id, input),
    onSuccess: () => {
      setError(null)
      setIsEditing(false)
      invalidate()
    },
    onError: (err) => setError((err as unknown as ApiError).message),
  })

  const toggleMutation = useMutation({
    mutationFn: () =>
      todosApi.update(todo.id, {
        title: todo.title,
        description: todo.description,
        isCompleted: !todo.isCompleted,
      }),
    onSuccess: invalidate,
    onError: (err) => setError((err as unknown as ApiError).message),
  })

  const deleteMutation = useMutation({
    mutationFn: () => todosApi.remove(todo.id),
    onSuccess: invalidate,
    onError: (err) => setError((err as unknown as ApiError).message),
  })

  function startEdit() {
    setDraftTitle(todo.title)
    setDraftDescription(todo.description ?? '')
    setError(null)
    setIsEditing(true)
  }

  function cancelEdit() {
    setError(null)
    setIsEditing(false)
  }

  function onSubmitEdit(e: FormEvent) {
    e.preventDefault()
    const title = draftTitle.trim()
    if (!title) {
      setError('Title is required.')
      return
    }
    updateMutation.mutate({
      title,
      description: draftDescription.trim() ? draftDescription.trim() : null,
      isCompleted: todo.isCompleted,
    })
  }

  function onDelete() {
    if (!confirm(`Delete "${todo.title}"?`)) return
    deleteMutation.mutate()
  }

  const busy = updateMutation.isPending || toggleMutation.isPending || deleteMutation.isPending

  if (isEditing) {
    return (
      <div className="todo-item">
        {error && <div className="error-banner" role="alert">{error}</div>}
        <form onSubmit={onSubmitEdit}>
          <label>
            Title
            <input
              type="text"
              value={draftTitle}
              onChange={(e) => setDraftTitle(e.target.value)}
              maxLength={200}
              autoFocus
              aria-label="Edit title"
            />
          </label>
          <label>
            Description
            <textarea
              value={draftDescription}
              onChange={(e) => setDraftDescription(e.target.value)}
              maxLength={2000}
              aria-label="Edit description"
            />
          </label>
          <div className="row">
            <div className="spacer" />
            <button type="button" className="secondary" onClick={cancelEdit} disabled={updateMutation.isPending}>
              Cancel
            </button>
            <button type="submit" disabled={updateMutation.isPending}>
              {updateMutation.isPending ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    )
  }

  return (
    <div className="todo-item">
      {error && <div className="error-banner" role="alert">{error}</div>}
      <div className="todo-row">
        <input
          type="checkbox"
          checked={todo.isCompleted}
          onChange={() => toggleMutation.mutate()}
          disabled={busy}
          aria-label={`Mark "${todo.title}" as ${todo.isCompleted ? 'incomplete' : 'complete'}`}
        />
        <span className={`todo-title${todo.isCompleted ? ' completed' : ''}`}>{todo.title}</span>
        <div className="todo-actions">
          <button type="button" className="secondary" onClick={startEdit} disabled={busy}>
            Edit
          </button>
          <button type="button" className="danger" onClick={onDelete} disabled={busy}>
            {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
      {todo.description && <p className="todo-description">{todo.description}</p>}
    </div>
  )
}
