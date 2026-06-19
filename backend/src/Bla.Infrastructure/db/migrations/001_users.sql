-- Users table: account identity + credentials for the auth slice.
-- snake_case columns; PK uuid; unique email; password hash (never plaintext); UTC timestamp.
CREATE TABLE IF NOT EXISTS users (
    id            uuid        NOT NULL PRIMARY KEY,
    email         text        NOT NULL,
    password_hash text        NOT NULL,
    created_at    timestamptz NOT NULL DEFAULT now()
);

-- Email is unique and used as the login lookup key (normalized, lower-case, set by the app).
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (email);
