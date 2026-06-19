# CONVENTIONS

Code and project standards. When in doubt, match the surrounding code over these notes — but don't
contradict the dependency rule in [`ARCHITECTURE.md`](ARCHITECTURE.md).

## .NET / C#

- **Naming:** `PascalCase` for types/methods/properties; `camelCase` for locals/parameters;
  `_camelCase` for private fields; `IName` for interfaces; async methods end in `Async`.
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`); treat warnings as errors
  where practical. No `null`-bang (`!`) to silence — handle the null.
- **Async all the way:** repositories and services are `async`/`await`, return `Task<T>`, accept a
  `CancellationToken` where it flows naturally. No `.Result` / `.Wait()`.
- **One type per file**, file named after the type.
- **DTOs are records**; domain entities are classes with guarded invariants. Never expose domain
  entities directly over HTTP — map to/from DTOs in the Application layer.
- **Dependency injection** for everything crossing a layer boundary; constructor injection,
  `readonly` fields. No service locator, no statics for stateful collaborators.
- **No business logic in controllers.** Controllers validate the request shape, call an Application
  service, and translate the result to an HTTP response. Business rules live in Application/Domain.

Layer boundaries (which project may reference which) are defined by the dependency rule in
[`ARCHITECTURE.md`](ARCHITECTURE.md) — follow that direction.

## Data access (Npgsql)

- **Always parameterized SQL** — never interpolate values into the command text.
- Use `NpgsqlDataSource` injected via DI; `await using` connections/commands/readers so they return
  to the pool.
- Map `DbDataReader` → domain objects explicitly in the repository (small private `Map(...)`
  helper per entity is fine).
- SQL lives in the repository (or a sibling `*.sql` for migrations/seed), not leaked outward.

## API design

- REST, resource-oriented routes (`/api/tasks`, `/api/auth`). Correct verbs and status codes:
  `200/201/204` success, `400` validation, `401` unauthenticated, `403` forbidden, `404` not found.
- Validation failures return a consistent problem shape (`ProblemDetails`).
- Every endpoint is annotated so the **OpenAPI document** is accurate (response types, auth
  requirements) — the generated Angular client is only as good as the contract.

## Testing (TDD)

- **Write the failing test first**, then the minimal code to pass, then refactor.
- xUnit + FluentAssertions; mock collaborators with NSubstitute (or Moq — pick one and stay
  consistent). Test naming: `Method_Scenario_ExpectedResult`.
- Domain and Application are unit-tested in isolation (ports mocked). Infrastructure repositories
  get integration tests against a real Postgres (Testcontainers or the compose `db`). Api gets
  endpoint tests via `WebApplicationFactory`.
- Don't test the framework; test our rules and wiring.

## Frontend (Angular)

- Standalone components, organized by **feature folder** (`tasks/`, `auth/`); shared UI in
  `shared/`, cross-cutting singletons in `core/`.
- **Never hand-write API models or HTTP calls** — consume the generated client in
  `core/api/generated/`. If the contract changed, regenerate (see [`RUNBOOK.md`](RUNBOOK.md)).
- State kept simple and local to features (signals/services); no heavy state library unless a need
  is proven.
- Strict TypeScript; no `any`. Reactive forms with validation mirroring the API rules.
- Responsive and accessible; **no console warnings/errors** (an explicit rubric item).

## Commits

- Small, focused commits with imperative messages.
- See [`rules/task-approach.md`](rules/task-approach.md) and
  [`rules/definition-of-done.md`](rules/definition-of-done.md) for how to work and finish a task.
