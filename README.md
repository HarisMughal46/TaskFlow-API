# TaskFlow API

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet) ![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-5C2D91) ![EF Core](https://img.shields.io/badge/EF_Core-SQLite-003B57?logo=sqlite) ![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens) ![xUnit](https://img.shields.io/badge/tests-xUnit-green) ![License](https://img.shields.io/badge/license-MIT-blue)

A task-management REST API built with ASP.NET Core 10. Users register or log in with JWT auth and manage their own **projects** and **tasks**. Every query is scoped to the signed-in user, so nobody can see anyone else's data.

## Features
- JWT authentication (register / login), passwords hashed with ASP.NET Core `PasswordHasher`
- Projects and Tasks CRUD, scoped per user
- Tasks have a status (`Todo` / `InProgress` / `Done`), a priority (`Low` / `Medium` / `High`) and a due date
- `GET tasks` supports filtering (status, priority, search), sorting (`sortBy`, `desc`) and pagination (`page`, `pageSize`)
- DataAnnotations validation that returns RFC 7807 `ProblemDetails` (400)
- Global exception handler (`IExceptionHandler`) that returns ProblemDetails (500)
- EF Core + SQLite; the database is created and seeded with demo data on startup
- Swagger UI with a JWT **Authorize** button
- `/health` health check endpoint
- Structured JSON console logging
- Layered code: Controllers → Services → Data, with DTOs
- Integration tests (`WebApplicationFactory`) plus unit tests, and GitHub Actions CI

## Project structure
```
src/TaskFlow.Api
  Controllers/   Auth, Projects, Tasks
  Services/      AuthService, ProjectService, TaskService, GlobalExceptionHandler
  Data/          AppDbContext, DbSeeder
  DTOs/          Request/response records, TaskQuery, PagedResult
  Models/        User, Project, TaskItem, enums
tests/TaskFlow.Api.Tests
```

## Running locally
Requirements: .NET 10 SDK
```bash
dotnet run --project src/TaskFlow.Api --urls http://localhost:5080
```
- Swagger UI: http://localhost:5080/swagger
- Health: http://localhost:5080/health
- Demo user: `demo@taskflow.dev` / `Demo@12345`

### Configuration
The JWT signing key is read from `Jwt:Key`. `appsettings.Development.json` holds a **dev-only placeholder**. For any real deployment, set your own key (at least 32 bytes) with an environment variable:
```bash
export TASKFLOW_JWT_KEY="your-long-random-secret"   # or Jwt__Key
```
Change the connection string with `ConnectionStrings__Default`.

## API endpoints
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | – | Register and receive a JWT |
| POST | `/api/auth/login` | – | Log in and receive a JWT |
| GET | `/api/projects` | ✔ | List my projects |
| GET | `/api/projects/{id}` | ✔ | Get one project |
| POST | `/api/projects` | ✔ | Create a project |
| PUT | `/api/projects/{id}` | ✔ | Update a project |
| DELETE | `/api/projects/{id}` | ✔ | Delete a project (and its tasks) |
| GET | `/api/projects/{projectId}/tasks` | ✔ | List tasks (filter/sort/paginate) |
| GET | `/api/projects/{projectId}/tasks/{id}` | ✔ | Get one task |
| POST | `/api/projects/{projectId}/tasks` | ✔ | Create a task |
| PUT | `/api/projects/{projectId}/tasks/{id}` | ✔ | Update a task |
| DELETE | `/api/projects/{projectId}/tasks/{id}` | ✔ | Delete a task |
| GET | `/health` | – | Health check |

Task query parameters: `status`, `priority`, `search`, `sortBy` (`title|dueDate|priority|createdAt|status`), `desc`, `page` (≥1), `pageSize` (1–100).

## Example requests
```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@taskflow.dev","password":"Demo@12345"}' | jq -r .token)

# Create a project
curl -X POST http://localhost:5080/api/projects -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"name":"Side project","description":"Weekend work"}'

# Create a task
curl -X POST http://localhost:5080/api/projects/3/tasks -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Ship v1","status":"InProgress","priority":"High","dueDate":"2026-12-01T00:00:00Z"}'

# List high-priority tasks, sorted by due date
curl "http://localhost:5080/api/projects/3/tasks?priority=High&sortBy=dueDate&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

## Tests
```bash
dotnet test
```
Covers: 401 without a token, login/register (including duplicates and bad credentials), 400 validation with ProblemDetails, project and task CRUD, user data isolation, filtering, sorting and pagination, plus unit tests for the query logic.

## License
MIT. See [LICENSE](LICENSE).
