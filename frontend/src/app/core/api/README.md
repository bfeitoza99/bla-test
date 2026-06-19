# `core/api/` — generated OpenAPI client

This folder is reserved for the **generated** Angular HTTP layer.

- `generated/` (git-ignored, regenerable) holds the typed services + models
  emitted by `openapi-generator` (`typescript-angular`) from the API's OpenAPI
  contract at `http://localhost:8080/openapi/v1.json`.
- Generate/refresh it with `npm run generate:api` (the API must be running).
- **Never hand-write or edit files under `generated/`** — they are overwritten
  on every regeneration. If the client is wrong, fix the contract (API
  annotations/DTOs) and regenerate. See
  `ai-context/rules/canonical-files.md`.

The `generated/` directory does not exist yet — it is created the first time
`generate:api` runs against a live API. Feature components and the AuthService
will consume these generated services in a later wave.
