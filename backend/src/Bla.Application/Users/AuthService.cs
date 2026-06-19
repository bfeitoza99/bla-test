using Bla.Domain.Users;
using FluentValidation;

namespace Bla.Application.Users;

/// <summary>
/// Orchestrates the auth use cases over the domain and the ports. Holds no infrastructure
/// knowledge: hashing, persistence, and token issuing are all reached through interfaces.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = User.NormalizeEmail(request.Email);

        if (await _users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new EmailAlreadyInUseException();
        }

        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // The hasher takes a User only to satisfy the Identity PasswordHasher<T> contract; the
        // default PBKDF2 implementation salts randomly and ignores the instance's contents. Hash
        // against an interim user (a non-empty placeholder hash satisfies the invariant), then
        // build the persisted user with the real hash.
        var interim = User.Create(id, normalizedEmail, "pending", createdAt);
        var passwordHash = _passwordHasher.Hash(interim, request.Password);
        var user = User.Create(id, normalizedEmail, passwordHash, createdAt);

        await _users.AddAsync(user, cancellationToken);

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAtUtc);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = User.NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Generic failure for both unknown email and wrong password — never reveal which.
        if (user is null
            || !_passwordHasher.Verify(user, user.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAtUtc);
    }

    public async Task<UserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user?.ToResponse();
    }
}
