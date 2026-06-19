using Bla.Application.Users;
using Bla.Domain.Users;
using Identity = Microsoft.AspNetCore.Identity;

namespace Bla.Infrastructure.Security;

/// <summary>
/// <see cref="IPasswordHasher"/> implemented over ASP.NET Core Identity's
/// <see cref="Identity.PasswordHasher{TUser}"/> (PBKDF2 with a per-hash random salt).
/// Allowed under the exercise's ORM/mediator ban — it is a hashing primitive, not data access.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Identity.PasswordHasher<User> _inner = new();

    public string Hash(User user, string password) =>
        _inner.HashPassword(user, password);

    public bool Verify(User user, string passwordHash, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(user, passwordHash, providedPassword);
        return result is Identity.PasswordVerificationResult.Success
            or Identity.PasswordVerificationResult.SuccessRehashNeeded;
    }
}
