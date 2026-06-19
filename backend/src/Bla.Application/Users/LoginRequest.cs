namespace Bla.Application.Users;

/// <summary>Login request: the credentials to authenticate an existing account.</summary>
public sealed record LoginRequest(string Email, string Password);
