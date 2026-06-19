using System.Collections.Concurrent;
using Bla.Application.Users;
using Bla.Domain.Users;

namespace Bla.Api.Tests.Auth;

/// <summary>
/// In-memory <see cref="IUserRepository"/> for endpoint tests, so the register -> login -> /me flow
/// runs deterministically without a real PostgreSQL. Keyed by id, with an email index that mirrors
/// the unique-email constraint.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _byId = new();

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var user = _byId.Values.FirstOrDefault(u => u.Email == normalizedEmail);
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_byId.Values.Any(u => u.Email == user.Email))
        {
            throw new InvalidOperationException("Duplicate email.");
        }

        _byId[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var exists = _byId.Values.Any(u => u.Email == normalizedEmail);
        return Task.FromResult(exists);
    }
}
