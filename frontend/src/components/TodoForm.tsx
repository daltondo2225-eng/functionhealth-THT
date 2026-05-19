import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { todosApi } from '../api/todos'
import { ErrorBanner } from './ErrorBanner'
import type { ApiError } from '../api/client'

export function TodoForm() {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      todosApi.create({
        title: title.trim(),
        description: description.trim() ? description.trim() : null,
      }),
    onSuccess: () => {
      setTitle('')
      setDescription('')
      setError(null)
      queryClient.invalidateQueries({ queryKey: ['todos'] })
    },
    onError: (err) => setError((err as unknown as ApiError).message),
  })

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!title.trim()) {
      setError('Title is required.')
      return
    }
    mutation.mutate()
  }

  return (
    <div className="card todo-form">
      <ErrorBanner message={error} />
      <form onSubmit={onSubmit}>
        <label>
          New task
          <input
            type="text"
            placeholder="What needs doing?"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={200}
            aria-label="New task title"
          />
        </label>
        <label>
          Description <span className="muted">(optional)</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={2000}
            aria-label="New task description"
          />
        </label>
        <div className="row">
          <div className="spacer" />
          <button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Adding…' : 'Add task'}
          </button>
        </div>
      </form>
    </div>
  )
}
