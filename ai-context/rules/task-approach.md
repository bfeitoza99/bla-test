# Rule: task approach

How to work a task in this repo. The goal is predictable, reviewable changes that respect the
architecture — not improvisation.

## Before writing code

1. **Read the context.** `AGENTS.md` → `ARCHITECTURE.md`, `CONVENTIONS.md`, `RUNBOOK.md`, and these
   rules. Don't re-derive decisions already recorded.
2. **Locate the right layer.** Decide where the change belongs (Domain / Application /
   Infrastructure / Api / frontend) and confirm it doesn't violate the dependency rule.
3. **Restate the slice.** One vertical slice at a time (e.g., "create task" end-to-end) rather than
   half-finished horizontal layers.

## While working (TDD loop)

4. **Red:** write the failing test that expresses the behavior.
5. **Green:** write the minimal production code to pass.
6. **Refactor:** clean up with tests green. Keep SQL parameterized and entities behind DTOs.
7. **Keep the contract honest.** If you changed an endpoint or DTO, the OpenAPI document changed —
   plan to regenerate the Angular client.

## Finishing

8. Run the relevant checks (see [`definition-of-done.md`](definition-of-done.md)), or use the
   `rerun` skill to exercise what changed.

## Scope discipline

- Don't expand scope mid-task. If you spot unrelated issues, note them separately (e.g. the commit
  message or a follow-up) instead of fixing them inline.
- Prefer reusing existing helpers/ports over adding new ones; check
  [`canonical-files.md`](canonical-files.md) first.
