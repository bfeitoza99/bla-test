namespace Bla.Application.Users;

/// <summary>Registration request: the credentials for a new account.</summary>
public sealed record RegisterRequest(string Email, string Password);
