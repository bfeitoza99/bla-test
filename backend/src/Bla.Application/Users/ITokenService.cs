using Bla.Domain.Users;

namespace Bla.Application.Users;

/// <summary>
/// Port for issuing access tokens. Implemented in Infrastructure as a signed JWT whose subject
/// claim carries the user's id (so the API's <c>GetUserId()</c> can read it back).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Issues a token for the user, returning the encoded token and its UTC expiry.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
