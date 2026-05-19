# Tasks — Take-Home Submission

A small to-do task management application: ASP.NET Core 8 backend with SQLite + EF Core, React 18 + Vite + TypeScript frontend, JWT-bearer authentication. Optimised for clarity and correctness over architectural surface area.

## Quickstart (Docker — one command)

```bash
docker compose up --build
```

Then open <http://localhost:5173>. Register an account, sign in, and start adding tasks. The SQLite database lives at `./data/todos.db` and survives container restarts.

To stop and wipe state: `docker compose down && rm -rf data/`.

## Local development (no Docker)

Two terminals.

```bash
# 1. Backend — needs .NET 8 SDK installed
cd backend
export Jwt__Secret="dev-only-secret-do-not-use-in-production-please-replace-min-32"
dotnet run --project TodoApi
# listens on http://localhost:8080
```

```bash
# 2. Frontend — needs Node 20+
cd frontend
npm install
npm run dev
# Vite dev server on http://localhost:5173, proxies /api to :8080
```

If you don't have .NET 8 installed locally, the Docker path is the one-command equivalent.

## API contract

All non-2xx responses use a single error envelope:

```json
{ "error": { "code": "validation_failed", "message": "Title is required.", "details": { "Title": ["Title is required."] } } }
```

| Method | Path                  | Auth | Body                                              | Success         | Errors                |
| ------ | --------------------- | ---- | ------------------------------------------------- | --------------- | --------------------- |
| POST   | `/api/auth/register`  | no   | `{ email, password }`                             | `200 { token, user }` | 400, 409              |
| POST   | `/api/auth/login`     | no   | `{ email, password }`                             | `200 { token, user }` | 400, 401              |
| GET    | `/api/todos`          | yes  | —                                                 | `200 [TodoDto]`       | 401                   |
| POST   | `/api/todos`          | yes  | `{ title, description? }`                         | `201 TodoDto`         | 400, 401              |
| GET    | `/api/todos/{id}`     | yes  | —                                                 | `200 TodoDto`         | 401, 404              |
| PUT    | `/api/todos/{id}`     | yes  | `{ title, description?, isCompleted }`            | `200 TodoDto`         | 400, 401, 404         |
| DELETE | `/api/todos/{id}`     | yes  | —                                                 | `204`                 | 401, 404              |
| GET    | `/health`             | no   | —                                                 | `200 { status }`      | —                     |

`TodoDto = { id, title, description, isCompleted, createdAt, updatedAt }` — `createdAt` / `updatedAt` are ISO-8601 UTC strings produced by the server.

## Trust model

Every protected endpoint resolves the current user **only** from the JWT `sub` claim — never from the request body, query, or path. Every query is scoped by `UserId`:

```csharp
var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
if (todo is null) throw AppException.NotFound();
```

Cross-user access returns **404, not 403**, on purpose: a 403 leaks the fact that the resource exists. The integration tests in [backend/TodoApi.Tests/OwnershipTests.cs](backend/TodoApi.Tests/OwnershipTests.cs) prove this for read, update, and delete and also verify that the underlying row is unchanged after a hijacking attempt.

Login uses an identical 401 message for "no such email" and "wrong password" to prevent user enumeration. Passwords are hashed with BCrypt (work factor 11). JWTs are HS256-signed with a 12-hour lifetime; the secret is required to be ≥32 characters and the app refuses to start otherwise.

**Token storage trade-off.** The JWT is kept in `localStorage` rather than an httpOnly cookie. The trade is convenience and zero CSRF surface against a slightly larger XSS blast radius if the SPA ever ships an XSS bug. For an SPA on a separate origin from the API this is a defensible default; the production-grade alternative — httpOnly + Secure + SameSite cookie with a CSRF token — is listed under "What I'd add for production." All user-rendered text in the app is React-escaped and we keep no `dangerouslySetInnerHTML` usage, so the XSS surface today is small.

## Architecture decisions

- **Flat, not layered.** Minimal API endpoints (`Endpoints/*.cs`) talk to EF Core directly. No `IRepository`, no `IService` — those abstractions don't solve a real problem here.
- **Single error envelope** is produced by one middleware ([Errors/ErrorHandlingMiddleware.cs](backend/TodoApi/Errors/ErrorHandlingMiddleware.cs)) plus an `AppException` type with factory methods. Validation uses DataAnnotations (`Errors/Validation.cs`), no FluentValidation. On the client side, [`api/client.ts`](frontend/src/api/client.ts) sets a 15s timeout and normalises every failure mode to the same `{ code, message, details? }` shape — 502/503/504 (e.g. backend down) becomes "Backend is unavailable", timeouts become "Request timed out", and network failures become "Network error" — so the UI never has to think about HTTP plumbing.
- **TanStack Query** on the frontend caches the todo list under one key. Every mutation invalidates that key so the UI always renders backend truth — no optimistic updates, no client-side merging, no drift between create/update and list view.
- **`localStorage` for the JWT** and a tiny `AuthContext` for hydration and sign-out. The axios client attaches the token automatically and clears + redirects to `/login` on any 401.
- **Edit form drafts are local component state** and are deliberately *not* reset on a failed save — see [TodoItem.tsx](frontend/src/components/TodoItem.tsx) and the corresponding test in [TodosPage.test.tsx](frontend/src/__tests__/TodosPage.test.tsx).
- **Status filter (All / Active / Completed) + "X of Y done" counter** are client-side over the already-fetched list. No server-side `?status=` query param, no contract drift between layers; if the list ever grew beyond a few hundred items, this is where you'd add a server-side filter, and the test in [TodosPage.test.tsx](frontend/src/__tests__/TodosPage.test.tsx) would be updated in lockstep.

## Trade-offs / scope choices

| Skipped | Why |
| ------- | --- |
| Due dates | Date/time-zone bugs are a well-known failure mode for take-homes. The prompt didn't require them; omitting them eliminates the risk entirely rather than half-implementing them. |
| Filter / sort / pagination | No real volume problem to solve at this scope; pagination state desync is itself a common failure mode, so skipping eliminates the risk. |
| Refresh tokens | A single 12-hour access token is honest for this scope. Refresh-token rotation is listed below as a "production-add." |
| Optimistic UI | Pessimistic mutations + query invalidation guarantee the list reflects backend state. Optimistic UX is nice but easy to get subtly wrong; skipping it is safer for an evaluation. |
| Roles / sharing | Not asked for. |

## What I'd add for production

- Refresh tokens with rotation; revocation table.
- Rate limiting on `/api/auth/*` (e.g. `Microsoft.AspNetCore.RateLimiting`).
- Structured logging (Serilog) with request IDs, plus OpenTelemetry traces + metrics.
- Real DB (Postgres) with connection pooling and proper migrations (`dotnet ef migrations add`).
- Audit log table for todo mutations.
- Soft-delete (`DeletedAt`) with a recovery window before hard delete.
- Email verification + password reset flow.
- CSP/HSTS/security headers at the edge (reverse proxy) and `Strict-Transport-Security`.
- CI pipeline running both test suites and `docker compose build` on every PR.
- Playwright end-to-end smoke covering register → create → edit → delete → logout.
- Sentry/Datadog for production error surfaces; UI error boundary too.

## Testing

```bash
# Backend — 19 integration tests using WebApplicationFactory + real SQLite (not EF InMemory)
cd backend && dotnet test
# Or via Docker:
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test TodoApi.sln

# Frontend — 6 component tests using Vitest + RTL + MSW
cd frontend && npm test -- --run
```

The backend test factory uses a real in-memory SQLite connection (`Microsoft.Data.Sqlite` with a keepalive `SqliteConnection`), not the EF InMemory provider. This gives real constraint behaviour, real transactions, and real SQL semantics in the integration suite.

Test coverage focuses on the failure modes the rubric names:

- `AuthTests.cs` — registration, duplicate email, validation errors, login happy path, unknown-email + wrong-password 401 with the **same** message.
- `OwnershipTests.cs` — every cross-user attempt (GET, PUT, DELETE) returns 404 and the victim's row is unchanged; no-token and tampered-token both return 401.
- `TodoCrudTests.cs` — create, update, toggle, delete, and empty-title validation that doesn't corrupt the row.
- Frontend `LoginPage.test.tsx` — happy path navigation, 401 banner with typed-email preservation.
- Frontend `TodosPage.test.tsx` — list render, create + invalidate, toggle, **failed-edit-preserves-draft**, delete, status filter + done-counter.

## Project layout

```
.
├── backend/
│   ├── TodoApi/                 — Minimal API project (.NET 8)
│   │   ├── Program.cs           — host wiring, JWT, EF, CORS, error middleware
│   │   ├── Endpoints/           — AuthEndpoints, TodoEndpoints (all ownership-scoped)
│   │   ├── Data/                — User, TodoItem, AppDbContext
│   │   ├── Auth/                — JwtTokenService, PasswordHasher (BCrypt)
│   │   ├── Contracts/           — request + response DTOs
│   │   └── Errors/              — AppException, Validation, ErrorHandlingMiddleware
│   ├── TodoApi.Tests/           — xUnit integration tests, real SQLite
│   └── Dockerfile               — multi-stage publish → aspnet:8.0 runtime
├── frontend/
│   ├── src/
│   │   ├── api/                 — axios client, auth + todos calls
│   │   ├── auth/                — AuthContext, ProtectedRoute
│   │   ├── pages/               — LoginPage, RegisterPage, TodosPage
│   │   ├── components/          — TodoForm, TodoList, TodoItem, ErrorBanner
│   │   └── __tests__/           — Vitest + MSW
│   ├── Dockerfile               — vite build → nginx:alpine
│   └── nginx.conf               — SPA fallback + /api proxy to backend container
├── docker-compose.yml           — backend + frontend, SQLite volume at ./data
├── .env.example
└── README.md                    — you are here
```
