-- Tasks table: a user's own units of work for the Tasks CRUD slice.
-- snake_case columns; PK uuid; nullable description; status stored as text (enum name);
-- UTC timestamps; user_id FK -> users(id) with cascade delete so a removed account takes its
-- tasks with it. Tasks are always queried scoped by user_id, so that column is indexed
-- (and (user_id, status) for the status-filtered list).
CREATE TABLE IF NOT EXISTS tasks (
    id          uuid        NOT NULL PRIMARY KEY,
    title       text        NOT NULL,
    description text        NULL,
    status      text        NOT NULL,
    due_date    timestamptz NOT NULL,
    user_id     uuid        NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

-- "Tasks by user" is the hot path; the composite covers the status-filtered list too.
CREATE INDEX IF NOT EXISTS ix_tasks_user_id ON tasks (user_id);
CREATE INDEX IF NOT EXISTS ix_tasks_user_id_status ON tasks (user_id, status);
