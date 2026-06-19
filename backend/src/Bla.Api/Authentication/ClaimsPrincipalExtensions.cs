using System.Security.Claims;

namespace Bla.Api.Authentication;

/// <summary>
/// Helpers for reading the authenticated identity off a <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the authenticated user's id, read from the token's subject claim
    /// (<see cref="ClaimTypes.NameIdentifier"/> or the JWT <c>sub</c> claim).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no user id claim is present or it is not a valid <see cref="Guid"/>.
    /// Endpoints that call this are expected to be behind <c>[Authorize]</c>, so a missing or
    /// malformed claim indicates a misconfiguration rather than an anonymous request.
    /// </exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (Guid.TryParse(value, out var userId))
        {
            return userId;
        }

        throw new InvalidOperationException(
            "The authenticated principal does not carry a valid user id claim.");
    }

    /// <summary>
    /// Attempts to read the authenticated user's id. Returns <see langword="false"/> when the
    /// principal is unauthenticated or the claim is missing/malformed, without throwing.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;
        if (principal is null)
        {
            return false;
        }

        var value =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }
}
