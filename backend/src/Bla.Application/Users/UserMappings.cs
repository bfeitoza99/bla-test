using Bla.Domain.Users;

namespace Bla.Application.Users;

/// <summary>
/// Hand-written mapping from the <see cref="User"/> domain entity to its HTTP-facing DTO.
/// Deliberately omits the password hash — it must never leave the server.
/// </summary>
public static class UserMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Email, user.CreatedAt);
}
