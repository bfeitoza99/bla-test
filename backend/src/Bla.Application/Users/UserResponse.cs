namespace Bla.Application.Users;

/// <summary>Public view of a user. Never carries the password hash.</summary>
public sealed record UserResponse(Guid Id, string Email, DateTime CreatedAt);
