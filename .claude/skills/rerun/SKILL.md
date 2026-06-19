---
name: rerun
description: Detect what changed in the BLA project since the last run (db / api / OpenAPI contract / web) and re-run only the affected parts via docker compose, regenerating the Angular OpenAPI client when the contract changed. Use when the user says "rerun", "run it again", "run the changed parts", or after editing code and wanting to see it live.
---

# rerun — run only what changed

Re-run the BLA app intelligently: figure out which areas changed, then run only the steps needed,
in dependency order (db → api → contract/client → web). Avoid full rebuilds when a narrow change
will do.

Authoritative context: [`AGENTS.md`](../../../AGENTS.md) and
[`ai-context/RUNBOOK.md`](../../../ai-context/RUNBOOK.md). The compose services are `db`, `api`,
`web` (see RUNBOOK).

## 1. Determine what changed

Compute the set of changed files since the last successful rerun:

1. Read the marker `.claude/.rerun-state` (a git SHA) if it exists.
2. Changed files =
   - uncommitted changes: `git status --porcelain` (and `git diff --name-only HEAD`), **plus**
   - committed since the marker: `git diff --name-only <marker>..HEAD` (if a marker exists).
3. Fallbacks:
   - Not a git repo, or no marker, or first run → treat **all areas** as changed.
   - If the projects/compose don't exist yet (scaffold not built), say "nothing to run yet —
     backend/frontend not scaffolded" and stop.

## 2. Classify changes → areas

Map changed paths to areas (a change can hit several):

| Changed path pattern                                              | Area(s) affected            |
|------------------------------------------------------------------|-----------------------------|
| `backend/src/Bla.Infrastructure/db/**`, `**/*.sql`               | **db** (migrate + seed)     |
| `backend/src/Bla.Api/**Controller*.cs`, `**/Dtos/**`, endpoint/DTO changes | **api** + **contract** |
| any other `backend/**/*.cs`, `*.csproj`                          | **api**                     |
| `frontend/**` (excluding generated client)                       | **web**                     |
| `docker-compose.yml`, any `Dockerfile`                           | **full rebuild**            |

"contract" = the OpenAPI document likely changed → the Angular client must be regenerated.

## 3. Run only the affected areas (in order)

Use the commands from `RUNBOOK.md`. Skip any area not flagged.

1. **Full rebuild** (if compose/Dockerfiles changed):
   `docker compose up -d --build` → done (covers everything), then go to step 4.
2. **db:** `docker compose up -d db`, then apply migrations + seed (startup migrator, or the
   project's migrate command). Wait until healthy.
3. **api:** `docker compose up -d --build api`. Wait for `http://localhost:8080/openapi/v1.json`
   to respond (poll briefly).
4. **contract → Angular client:** if `contract` was flagged and the api is up, regenerate:
   from `frontend/`, `npm run generate:api` (reads `http://localhost:8080/openapi/v1.json`).
5. **web:** `docker compose up -d --build web` (or, for local dev, `npm start` in `frontend/`).

After starting, surface the URLs: API `:8080`, Scalar `:8080/scalar`, frontend `:4200`.

## 4. Verify and record

- Confirm the affected services are up (`docker compose ps`) and the API health/OpenAPI responds.
- If anything failed, report the actual error and which step — don't claim success.
- On success, update the marker: write the current `git rev-parse HEAD` to `.claude/.rerun-state`
  (only if in a git repo). This file is git-ignored.

## Notes

- Prefer the narrowest action: a pure frontend edit should **not** rebuild the api.
- Never edit generated client files; regenerate from the contract instead
  (see [`canonical-files.md`](../../../ai-context/rules/canonical-files.md)).
- Commands assume Windows + PowerShell + Docker Desktop.
