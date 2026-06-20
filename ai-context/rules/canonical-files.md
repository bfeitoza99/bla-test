# Rule: canonical files

Some things have a **single source of truth**. Don't duplicate, fork, or hand-maintain a parallel
copy — change the canonical source and let everything else derive from it.

| Concern                         | Canonical source                                      | Do NOT duplicate by…                                  |
|---------------------------------|-------------------------------------------------------|-------------------------------------------------------|
| Agent instructions / rules      | `AGENTS.md` + `ai-context/`                            | …copying rules into `CLAUDE.md` or other bridge files |
| API contract                    | The running API's OpenAPI doc (`/openapi/v1.json`)     | …hand-writing TypeScript models or HTTP calls         |
| Frontend API models & services  | Generated client in `frontend/src/app/core/api/generated/` | …editing generated files (they're regenerated)   |
| Database schema                 | SQL migration scripts in `Bla.Infrastructure/db/migrations/` | …mutating the DB manually outside a script       |
| Domain rules/invariants         | `Bla.Domain` entities                                  | …re-validating differently in controllers or the UI   |
| Run/build instructions          | `ai-context/RUNBOOK.md`                                | …scattering setup steps across multiple READMEs       |

## Rules

- **Generated code is read-only.** If the generated Angular client is wrong, fix the **contract**
  (API annotations/DTOs) and regenerate — never edit files under `core/api/generated/`. The client
  **is committed** (so a clean clone and the `web` Docker build work without a generation step), but
  it is still generated: regenerate with `npm run generate:api` (runs openapi-generator via Docker,
  no local Java needed), don't hand-edit.
- **Bridge files stay thin.** `CLAUDE.md` (and any future `GEMINI.md`, `.cursorrules`, etc.) only
  redirect to `AGENTS.md`. No rules live in them.
- **Schema changes go through migrations**, never ad-hoc `ALTER`s on a running DB.
- Validation logic has one home (Domain/Application). The UI may mirror it for UX, but the server
  is authoritative.
