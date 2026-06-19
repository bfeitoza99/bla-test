namespace Bla.Api.Authentication;

/// <summary>
/// JWT bearer settings, bound from the <c>Jwt</c> configuration section.
/// The signing key is supplied via configuration/environment — never hard-coded.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (<c>iss</c>).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Intended audience (<c>aud</c>).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key. Must be strong and supplied from config; no shipped default.</summary>
    public string SigningKey { get; set; } = string.Empty;
}
