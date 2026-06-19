using System.Security.Claims;
using System.Text;
using Bla.Domain.Users;
using Bla.Infrastructure.Security;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bla.Infrastructure.Tests.Security;

/// <summary>
/// The issued JWT must (a) carry the user's id in a claim the API's <c>GetUserId()</c> reads
/// (<see cref="ClaimTypes.NameIdentifier"/> / <c>sub</c>), and (b) validate against the same
/// issuer/audience/signing-key parameters the foundation configures.
/// </summary>
public class JwtTokenServiceTests
{
    private const string SigningKey = "test-only-signing-key-0123456789abcdef0123456789";
    private const string Issuer = "bla-api-test";
    private const string Audience = "bla-spa-test";

    private static JwtTokenOptions Options() => new()
    {
        Issuer = Issuer,
        Audience = Audience,
        SigningKey = SigningKey,
        Lifetime = TimeSpan.FromHours(1),
    };

    private static TokenValidationParameters FoundationValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    [Fact]
    public async Task CreateToken_ProducesTokenValidatableByFoundationParameters()
    {
        var service = new JwtTokenService(Options());
        var user = User.Create(Guid.NewGuid(), "user@bla.local", "hash", DateTime.UtcNow);

        var (token, expiresAtUtc) = service.CreateToken(user);

        expiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, FoundationValidationParameters());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateToken_PutsUserIdInNameIdentifierAndSubClaims()
    {
        var service = new JwtTokenService(Options());
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "user@bla.local", "hash", DateTime.UtcNow);

        var (token, _) = service.CreateToken(user);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, FoundationValidationParameters());
        var identity = (ClaimsIdentity)result.ClaimsIdentity;

        // The same lookup ClaimsPrincipalExtensions.GetUserId() performs.
        var principal = new ClaimsPrincipal(identity);
        var nameId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        nameId.Should().Be(userId.ToString());
    }
}
