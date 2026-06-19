namespace Bla.Application.Users;

/// <summary>
/// Use-case service for the auth slice: register a new account, authenticate and issue a token,
/// and fetch the current user. Input validation, duplicate-email rejection, and credential
/// verification all live here so controllers stay thin.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new account and issues a token. Validates input (throws
    /// <see cref="FluentValidation.ValidationException"/>) and rejects a duplicate email
    /// (throws <see cref="EmailAlreadyInUseException"/>).
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates and issues a token. Validates input shape, then throws
    /// <see cref="InvalidCredentialsException"/> on any authentication failure (generic by design).
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user for the given id, or <see langword="null"/> if no such user exists.
    /// </summary>
    Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
