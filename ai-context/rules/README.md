# rules/

Detailed behavior rules for agents working in this repo. These are read on the first interaction of
a session (see [`AGENTS.md`](../../AGENTS.md)). They are split into small files so `AGENTS.md` stays
a lean index.

- [`task-approach.md`](task-approach.md) — how to pick up and work a task.
- [`definition-of-done.md`](definition-of-done.md) — the checklist for "done".
- [`canonical-files.md`](canonical-files.md) — single-source-of-truth files; don't fork or
  duplicate them.
- [`security.md`](security.md) — auth, secrets, validation, and data-access safety.

If a rule here conflicts with [`ARCHITECTURE.md`](../ARCHITECTURE.md) or
[`CONVENTIONS.md`](../CONVENTIONS.md), the architecture/convention wins and this file should be
fixed.
