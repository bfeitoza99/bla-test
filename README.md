# BLA — Task Management (.NET Technical Interview Exercise)

A task-management application: **.NET 10 API** in Clean Architecture + **Angular frontend**.

> **For AI agents:** start with [`AGENTS.md`](./AGENTS.md). All architecture, conventions, and run
> instructions live in [`ai-context/`](ai-context/).

## Stack

| Layer       | Technology                                                              |
|-------------|-------------------------------------------------------------------------|
| Backend     | .NET 10, ASP.NET Core Web API, Clean Architecture, TDD (xUnit)           |
| Data        | PostgreSQL + **Npgsql** (raw ADO.NET — no EF/Dapper/Mediator)            |
| API surface | Native OpenAPI, exposed via **Scalar** at `/scalar`                      |
| Frontend    | Angular (services/models **generated** from the OpenAPI contract)       |
| Infra       | Docker (one Dockerfile per service) + `docker-compose.yml`              |

## Running

The full walkthrough (build, tests, migrations, seed, client generation) is in
[`ai-context/RUNBOOK.md`](ai-context/RUNBOOK.md). Quick start:

```bash
docker compose up --build      # brings up db + api + web
# API:      http://localhost:8080
# Scalar:   http://localhost:8080/scalar
# OpenAPI:  http://localhost:8080/openapi/v1.json
# Frontend: http://localhost:4200
```

## Demo credentials

Demo data and a demo user are created by the seed (see
[`RUNBOOK.md`](ai-context/RUNBOOK.md)).

## User story

> _As a user, I want to create an account and manage my tasks (create, list, update, and delete),
> each with a title, description, status, and due date, so I can keep track of what I need to do._

(Expanded in the presentation — see [`ai-context/ARCHITECTURE.md`](ai-context/ARCHITECTURE.md).)

## GenAI tooling

How this project was built with an AI coding tool — the prompts, a representative output sample, and
how the AI's suggestions were validated, corrected, and hardened — is in [`docs/genai.md`](docs/genai.md).
