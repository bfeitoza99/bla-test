using System.Security.Claims;
using System.Text;
using Bla.Application.Users;
using Bla.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bla.Infrastructure.Security;

/// <summary>
/// Issues a signed JWT for a <see cref="User"/>. The token's subject is the user's id, written to
/// both the <c>sub</c> claim and <see cref="ClaimTypes.NameIdentifier"/> so the API's
/// <c>ClaimsPrincipal.GetUserId()</c> reads it back regardless of claim-mapping behavior. Issuer,
/// audience, and signing key match the foundation's validation parameters exactly.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtTokenOptions _options;

    public JwtTokenService(JwtTokenOptions options)
    {
        _options = options;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.Add(_options.Lifetime);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var userId = user.Id.ToString();

        // Only non-sensitive claims. Never the password hash.
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId,
            [ClaimTypes.NameIdentifier] = userId,
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            Claims = claims,
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return (token, expiresAtUtc);
    }
}
