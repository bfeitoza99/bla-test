namespace Bla.Domain.Users;

/// <summary>
/// A registered account. Identity plus the credentials needed to authenticate: a unique,
/// normalized (trimmed, lower-case) email and a password <em>hash</em> (never plaintext).
/// Invariants are guarded at construction; the type carries no I/O and no framework dependency.
/// </summary>
public sealed class User
{
    private User(Guid id, string email, string passwordHash, DateTime createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    /// <summary>Primary key.</summary>
    public Guid Id { get; }

    /// <summary>Normalized (trimmed, lower-case) email; unique across users.</summary>
    public string Email { get; }

    /// <summary>The PBKDF2 password hash. Never the plaintext password.</summary>
    public string PasswordHash { get; }

    /// <summary>Creation timestamp, always UTC.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Creates a new user, enforcing invariants and normalizing the email to trimmed lower-case.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the id is empty, the email or hash is missing, or <paramref name="createdAt"/> is not UTC.
    /// </exception>
    public static User Create(Guid id, string email, string passwordHash, DateTime createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email must not be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash must not be empty.", nameof(passwordHash));
        }

        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("CreatedAt must be UTC.", nameof(createdAt));
        }

        return new User(id, NormalizeEmail(email), passwordHash, createdAt);
    }

    /// <summary>
    /// Rehydrates a user from a trusted store (e.g. the database) without re-normalizing the email,
    /// while still guarding the non-empty invariants.
    /// </summary>
    public static User Restore(Guid id, string email, string passwordHash, DateTime createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email must not be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash must not be empty.", nameof(passwordHash));
        }

        return new User(id, email, passwordHash, createdAt);
    }

    /// <summary>Normalizes an email for storage and comparison: trimmed and lower-cased.</summary>
    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
