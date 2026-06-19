# Rule: definition of done

A change is **done** only when all of the following hold. These map directly to the exercise's
evaluation criteria (Clean Architecture, testing, code quality, functionality, no console
warnings).

## Backend

- [ ] New behavior is covered by tests written **test-first** (TDD), and `dotnet test` is green.
- [ ] No layer-boundary violations: Domain depends on nothing; Application has no Npgsql/ASP.NET;
      Infrastructure is the only project importing Npgsql; Api is the only composition root.
- [ ] All SQL is **parameterized**; connections/commands/readers are disposed (`await using`).
- [ ] Endpoints use correct verbs/status codes and return DTOs (never domain entities).
- [ ] Build is warning-clean (nullable warnings addressed, not suppressed).

## Contract & frontend

- [ ] If endpoints/DTOs changed, the **OpenAPI document is correct** and the Angular client was
      **regenerated** (`npm run generate:api`).
- [ ] Frontend builds and lints clean; the feature works against the running API.
- [ ] **No errors or warnings in the browser console** (explicit rubric item).
- [ ] UI is responsive and the CRUD flow works end-to-end.

## Security

- [ ] Authorized endpoints reject missing/invalid tokens; users can only access their own tasks.
- [ ] No secrets committed; passwords are hashed, never stored or logged in plaintext.
      (See [`security.md`](security.md).)

If any box can't be checked, the task is not done — say so explicitly rather than implying success.
