# AGENTS.md — Source of truth

This file is the repo's **always-on index**. Read it on **every interaction**, before doing
anything. It is short on purpose: the dense context lives in [`ai-context/`](ai-context/), which
must be read **once, at the start of a session**.

## What this project is

`bla-test` is the **BLA .NET Technical Interview Exercise** — a **task management** application:

- **.NET 10 backend** in **Clean Architecture**, driven by **TDD**.
- **Task CRUD** (Web API) + a **second user API** (signup, login, authorized and non-authorized
  endpoints via **JWT**).
- **Responsive Angular frontend**, with **services and models generated from the OpenAPI** contract.
- The API is exposed/explored through **Scalar** (not Swagger UI), on top of ASP.NET Core's
  **native OpenAPI**.

## Required reading (context layer)

On the **first interaction of a session**, read in this order:

1. [`ai-context/ARCHITECTURE.md`](ai-context/ARCHITECTURE.md) — layers, dependencies, decisions.
2. [`ai-context/CONVENTIONS.md`](ai-context/CONVENTIONS.md) — code and naming standards.
3. [`ai-context/RUNBOOK.md`](ai-context/RUNBOOK.md) — run, test, migrate, seed, generate client.
4. [`ai-context/rules/`](ai-context/rules/) — detailed behavior rules:
   - [`task-approach.md`](ai-context/rules/task-approach.md)
   - [`definition-of-done.md`](ai-context/rules/definition-of-done.md)
   - [`canonical-files.md`](ai-context/rules/canonical-files.md)
   - [`security.md`](ai-context/rules/security.md)

## Key rules (summary — detail lives in `ai-context/`)

- **Don't invent architecture.** Follow the layers and dependency direction in `ARCHITECTURE.md`
  (dependencies always point inward; the Domain knows nothing about infrastructure).
- **TDD first.** Write the failing test before the production code.
- **No EF / Dapper / Mediator.** Data access is hand-written SQL behind *ports*.
- **The contract drives the frontend.** Changed an endpoint/DTO → **regenerate the Angular OpenAPI
  client**.

## Useful skill

- **`rerun`** ([`.claude/skills/rerun/`](.claude/skills/rerun/SKILL.md)) — detects what changed
  (db / api / OpenAPI contract / web) and re-runs only what's needed via `docker compose`.
