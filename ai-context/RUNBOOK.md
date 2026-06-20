# RUNBOOK

How to build, run, test, migrate, seed, and regenerate the OpenAPI client. Commands assume Windows
+ PowerShell (the dev machine) and Docker Desktop.

> Status: backend (auth + tasks), frontend, and Docker infra are implemented. Keep this file in
> sync as things change.

## Prerequisites

- **.NET 10 SDK**
- **Node.js LTS** + npm (for Angular and the OpenAPI generator)
- **Docker Desktop** (for PostgreSQL and the full compose run)
- Optional: `dotnet tool install -g Npgsql`-style tools are **not** used — schema is plain SQL.

## Service topology (docker-compose)

`docker-compose.yml` defines three services. The `rerun` skill and these docs assume these names:

| Service | What                          | Port (host) |
|---------|-------------------------------|-------------|
| `db`    | PostgreSQL                    | 5432        |
| `api`   | .NET 10 Web API               | 8080        |
| `web`   | Angular app                   | 4200        |

## Run everything (Docker)

```powershell
docker compose up --build
```

- API:      http://localhost:8080
- Scalar:   http://localhost:8080/scalar
- OpenAPI:  http://localhost:8080/openapi/v1.json
- Frontend: http://localhost:4200

Just the database (for local backend dev):

```powershell
docker compose up -d db
```

## Backend — local dev

```powershell
# from backend/
dotnet restore
dotnet build
dotnet run --project src/Bla.Api      # serves API + Scalar + OpenAPI
```

Connection string comes from configuration/env (`ConnectionStrings__Default`), pointing at the
compose `db`. Never commit real secrets — use `.env` (git-ignored) from `.env.example`.

## Database — migrations & seed

- Schema lives as plain SQL under `backend/src/Bla.Infrastructure/db/migrations/*.sql`.
- A lightweight migrator runs pending scripts on API startup (idempotent, tracked in a
  `schema_migrations` table).
- The **seed** creates a demo user and sample tasks:
  - email: `demo@bla.local` · password: `Demo123!` _(placeholder — confirm in seed script)_
- To reset: drop the `db` volume and bring it back up.

```powershell
docker compose down -v        # wipes the db volume
docker compose up --build db api
```

## Tests (TDD)

```powershell
# from backend/
dotnet test                                   # all layers
dotnet test tests/Bla.Domain.Tests            # a single project
```

Infrastructure integration tests need Postgres (Testcontainers spins one up, or point them at the
compose `db`).

## OpenAPI → Angular client generation

The Angular HTTP layer is **generated** from the live contract. Regenerate whenever an endpoint or
DTO changes.

```powershell
# API must be running (so /openapi/v1.json is available), then from frontend/
npm run generate:api
```

`generate:api` (`frontend/scripts/generate-api.mjs`) fetches `http://localhost:8080/openapi/v1.json`
and runs the **openapi-generator Docker image** (`typescript-angular`) — so **no local Java/JDK is
needed, only Docker**. Output goes to `src/app/core/api/generated/`, which **is committed** (so a
clean clone and the `web` Docker build work without a generation step); it's generated, so never
hand-edit it — change the API contract and regenerate.

## Frontend — local dev

```powershell
# from frontend/
npm install
npm start            # ng serve on http://localhost:4200
npm run build
npm run lint
```

## The `rerun` skill

Instead of remembering which of the above to run after a change, invoke the **`rerun`** skill — it
inspects what changed (db / api / contract / web) and runs only the affected steps. See
[`.claude/skills/rerun/SKILL.md`](../.claude/skills/rerun/SKILL.md).
