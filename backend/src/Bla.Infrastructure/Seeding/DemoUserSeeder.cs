using Bla.Application.Users;
using Bla.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Bla.Infrastructure.Seeding;

/// <summary>
/// Idempotent startup seed: ensures the demo account from the RUNBOOK
/// (<c>demo@bla.local</c> / <c>Demo123!</c>) exists, with the password hashed via the real
/// <see cref="IPasswordHasher"/>. A no-op when the account is already present.
/// </summary>
public sealed class DemoUserSeeder
{
    public const string DemoEmail = "demo@bla.local";
    public const string DemoPassword = "Demo123!";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DemoUserSeeder> _logger;

    public DemoUserSeeder(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ILogger<DemoUserSeeder> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(DemoEmail);

        if (await _users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogInformation("Demo user already present; skipping seed.");
            return;
        }

        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var interim = User.Create(id, normalizedEmail, "pending", createdAt);
        var passwordHash = _passwordHasher.Hash(interim, DemoPassword);
        var user = User.Create(id, normalizedEmail, passwordHash, createdAt);

        await _users.AddAsync(user, cancellationToken);
        _logger.LogInformation("Seeded demo user {Email}.", normalizedEmail);
    }
}
