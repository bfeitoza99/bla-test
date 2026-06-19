using Bla.Domain.Users;

namespace Bla.Application.Users;

/// <summary>
/// Port for password hashing/verification. Implemented in Infrastructure over ASP.NET Core
/// Identity's <c>PasswordHasher&lt;T&gt;</c> (PBKDF2). Plaintext passwords never leave this seam.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password for the given user, returning the encoded hash.</summary>
    string Hash(User user, string password);

    /// <summary>Verifies a plaintext password against a stored hash for the given user.</summary>
    bool Verify(User user, string passwordHash, string providedPassword);
}
