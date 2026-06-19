namespace Bla.Application.Users;

/// <summary>The issued access token and its UTC expiry, returned on register/login.</summary>
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc);
