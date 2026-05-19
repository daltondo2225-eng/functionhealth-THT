export function ErrorBanner({ message }: { message: string | null | undefined }) {
  if (!message) return null
  return (
    <div className="error-banner" role="alert">
      {message}
    </div>
  )
}
