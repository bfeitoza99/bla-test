# ARCHITECTURE

How the system is organized: layers, dependency direction, the main building blocks, and the key
design decisions. **Read this before writing code.**

## Scope

The product is driven by the user story in [`README.md`](../README.md): account creation/login plus
full CRUD on a user's own tasks. That splits into two bounded concerns — **Tasks** (the core CRUD
use case) and **Users/Auth** (account, login, and protecting the task endpoints).

## Clean Architecture — dependency rule

Dependencies **always point inward**. Inner layers know nothing about outer layers. The Domain has
zero dependencies; infrastructure details (Postgres, JWT, HTTP) live at the edges and are reached
only through interfaces (ports) defined further in.

```
            ┌─────────────────────────────────────────────┐
            │                  Bla.Api                      │  ASP.NET Core Web API
            │  controllers · DI composition root · auth ·   │  (depends on App + Infra)
            │  OpenAPI + Scalar · middleware                │
            └───────────────┬───────────────┬───────────────┘
                            │               │
                ┌───────────▼──────┐  ┌─────▼───────────────────┐
                │ Bla.Application  │  │   Bla.Infrastructure     │
                │ use-case services│  │  Npgsql repositories ·   │
                │ ports (interfaces)│ │  PasswordHasher · JWT ·  │
                │ DTOs · validation │ │  migrations · seed       │
                └───────────┬──────┘  └─────────┬───────────────┘
                            │                   │
                            └─────────┬─────────┘
                                ┌─────▼─────┐
                                │ Bla.Domain │  entities · enums · domain rules
                                │ (no deps)  │
                                └───────────┘
```

- **`Bla.Domain`** — entities (`TaskItem`, `User`), enums (`TaskStatus`), and invariants/domain
  rules. No framework, no I/O, no NuGet beyond the BCL.
- **`Bla.Application`** — use-case services that orchestrate the domain, the **ports**
  (`ITaskRepository`, `IUserRepository`, `IPasswordHasher`, `ITokenService`), request/response
  **DTOs**, and input validation. Depends on Domain only. **No Npgsql here.**
- **`Bla.Infrastructure`** — the only place that touches the outside world: Npgsql repository
  implementations (hand-written SQL), password hashing, JWT token issuing, DB migrations and seed.
  Depends on Application + Domain.
- **`Bla.Api`** — the composition root: controllers, DI wiring (picks the concrete repos),
  authentication/authorization, middleware, OpenAPI + Scalar. Depends on Application + Infra.

**Why this matters for the panel:** swapping Postgres for SQL Server or Mongo means writing one new
repository class in Infrastructure — Domain, Application, and Api don't move. That's the payoff of
the dependency rule and the demo talking point.

## The two APIs (per the exercise)

1. **Tasks API** — full CRUD, all endpoints **authorized** (a user only sees/edits their own tasks).
   - `GET /api/tasks` · `GET /api/tasks/{id}` · `POST /api/tasks` · `PUT /api/tasks/{id}` ·
     `DELETE /api/tasks/{id}`
2. **Users/Auth API** — account + session, with both authorized and non-authorized endpoints.
   - `POST /api/auth/register` (public) · `POST /api/auth/login` (public, returns a JWT in the
     response body) · `GET /api/auth/me` (authorized) · a public health/ping endpoint to
     demonstrate the non-authorized path.

## Data model

**`User`**: `Id` (Guid, PK) · `Email` (unique) · `PasswordHash` · `CreatedAt`.
**`TaskItem`**: `Id` (Guid, PK) · `Title` · `Description` (nullable) · `Status` (enum:
`Todo`/`InProgress`/`Done`) · `DueDate` · `UserId` (FK → User) · `CreatedAt` · `UpdatedAt`.

Two stores as required: a `users` table and a `tasks` table, each with a PK and ≥ 2 other fields.
Relationship is `tasks.user_id → users.id`; queries are "tasks by user" (an indexed column, not a
cross-aggregate join).

## Data access — raw ADO.NET via Npgsql

- Repositories implement Application ports with hand-written SQL — no ORM.
- Schema is created/evolved with **plain `.sql` migration scripts** run on startup (or via a small
  migrator), and a **seed** inserts the demo user + sample tasks.

Coding mechanics (`NpgsqlDataSource`, parameterized commands, manual `DbDataReader` mapping) live in
[`CONVENTIONS.md`](CONVENTIONS.md).

## API surface — OpenAPI + Scalar

- ASP.NET Core's **native OpenAPI** (`Microsoft.AspNetCore.OpenApi`) produces the document at
  `/openapi/v1.json`.
- **Scalar** (`Scalar.AspNetCore`, `MapScalarApiReference()`) renders the interactive reference at
  `/scalar`. No Swashbuckle/Swagger UI.
- This document is the **contract**: the Angular client is generated from it (see below), so the
  frontend never hand-writes HTTP calls or models.

## Frontend — Angular, generated from the contract

- Angular app under `frontend/`, standalone components, organized by feature.
- **Generated layer:** `openapi-generator` (`typescript-angular`) reads `/openapi/v1.json` and emits
  typed services + models into `frontend/src/app/core/api/generated/` (git-ignored, regenerable).
- Feature components depend on the generated services; auth is an HTTP interceptor that attaches the
  `Bearer` token (kept in `localStorage`) to outgoing requests.

## Auth & security (summary)

- **JWT bearer auth:** a single ~1h token returned at login, stored in `localStorage`, attached via
  an interceptor — a pragmatic choice for the demo, with a documented production upgrade path
  (`httpOnly` cookies + rotating refresh). Passwords hashed via ASP.NET Core Identity's
  `PasswordHasher<T>` (PBKDF2). Full detail in [`rules/security.md`](rules/security.md).

## Project layout

```
backend/
  src/
    Bla.Domain/
    Bla.Application/
    Bla.Infrastructure/
    Bla.Api/
  tests/
    Bla.Domain.Tests/
    Bla.Application.Tests/
    Bla.Infrastructure.Tests/
    Bla.Api.Tests/
frontend/                      # Angular app
docker-compose.yml             # services: db, api, web
```

## Decisions log

- **PostgreSQL + Npgsql (raw ADO.NET)** over an ORM — the exercise bans EF/Dapper/Mediator;
  hand-written SQL behind ports is the cleanest way to satisfy that while keeping the architecture
  clean.
- **Scalar over Swagger UI** — modern OpenAPI reference, pairs with .NET native OpenAPI.
- **Generated Angular client** — the OpenAPI contract is the single source of truth for the
  frontend's HTTP layer; no drift between API and client.
- **JWT bearer auth, single ~1h token in `localStorage`** — pragmatic, minimal moving parts for the
  demo; the XSS tradeoff and the `httpOnly`-cookie + rotating-refresh upgrade path are documented in
  [`rules/security.md`](rules/security.md). Tasks scoped to the authenticated user.
