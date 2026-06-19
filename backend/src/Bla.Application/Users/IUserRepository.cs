using Bla.Domain.Users;

namespace Bla.Application.Users;

/// <summary>
/// Persistence port for <see cref="User"/> aggregates. Implemented in Infrastructure with
/// hand-written, parameterized SQL. Email lookups assume a normalized (trimmed, lower-case) value.
/// </summary>
public interface IUserRepository
{
    /// <summary>Returns the user with the given normalized email, or <see langword="null"/>.</summary>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Returns the user with the given id, or <see langword="null"/>.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new user.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a user already exists with the given normalized email.</summary>
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
