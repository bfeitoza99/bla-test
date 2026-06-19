namespace Bla.Infrastructure.Security;

/// <summary>
/// JWT issuing settings, bound from the shared <c>Jwt</c> configuration section — the same section
/// the API uses to <em>validate</em> tokens, so issuer/audience/key stay in lock-step. The signing
/// key is supplied from config/environment; there is no shipped default.
/// </summary>
public sealed class JwtTokenOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Token lifetime. Defaults to ~1 hour per the foundation's documented choice.</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(1);
}
