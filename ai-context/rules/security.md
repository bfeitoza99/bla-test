# Rule: security

Auth, secrets, validation, and data-access safety. The exercise explicitly asks how authentication,
validation, and edge cases are handled — treat these as first-class.

## Authentication & authorization

**JWT bearer token, single token (~1h), kept in `localStorage` and attached by an HTTP
interceptor.** This is a deliberate *pragmatic* choice for the exercise: minimal moving parts so the
demo is reliable. The token carries the user id and only non-sensitive claims; login returns it in
the response body.

- Tasks endpoints are **authorized**; the user id comes from the validated token, **never** from a
  request body/query. A user can only read/modify **their own** tasks — scope every query by the
  authenticated user id (`WHERE user_id = @userId`) and reject access to another user's resource.
- Keep at least one **public** endpoint (e.g. health/ping) to demonstrate the non-authorized path.
- Return `401` for missing/invalid token, `403` for authenticated-but-not-allowed.

> **Known tradeoff (say this in the review):** `localStorage` is readable by JS, so a token is
> exposed to XSS, and a single ~1h token can't be revoked before it expires. The production upgrade
> path is `httpOnly` `Secure` `SameSite` cookies with a short-lived access token + a rotating,
> revocable refresh token (and CSRF defense). Chosen against here purely to keep the demo simple —
> the knowledge is what scores the points, not the extra plumbing.

## Passwords

- Hash with ASP.NET Core Identity's `PasswordHasher<T>` (PBKDF2) — allowed (not EF/Dapper/Mediator).
- **Never** store, log, or return password plaintext or the hash. No passwords in OpenAPI examples.
- Enforce a minimum password policy in validation; reject on registration.

## CORS

- The SPA (`http://localhost:4200`) and API (`http://localhost:8080`) are different origins, so the
  API must enable **CORS** for the SPA origin — otherwise every call fails with a console error
  (and the "no console errors" rubric item with it).
- Allow a **specific origin** (the SPA), not `AllowAnyOrigin()`. With bearer tokens we don't send
  cookies, so `AllowCredentials` is **not** required.
- Allow the methods/headers actually used (`Authorization`, `Content-Type`). Drive the allowed
  origin from config (it differs dev vs prod).

## Secrets & configuration

- No secrets in source. Connection strings, JWT signing keys, etc. come from environment/config.
- `.env` is git-ignored; commit only `.env.example` with placeholder values.
- The JWT signing key must be strong and supplied via config; never a hard-coded default in code
  that ships.

## Input validation & edge cases

- Validate all inbound DTOs (required fields, lengths, `DueDate` sanity, `Status` is a known enum
  value). Invalid input → `400` with `ProblemDetails`.
- **Data access:** every query parameterized (no string concatenation) → prevents SQL injection.
- Handle not-found (`404`) vs. forbidden (`403`) distinctly — don't leak existence of other users'
  resources.
- Be deliberate about concurrency and nulls; don't `!`-away nullability.

## Frontend

- Store the JWT in `localStorage` and attach it as a `Bearer` header via an Angular HTTP
  interceptor; clear it on logout/401. 
- Don't trust client-side validation alone — the server is authoritative.
- No secrets or keys baked into the Angular bundle.
