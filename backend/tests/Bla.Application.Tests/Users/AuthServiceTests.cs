using Bla.Application.Users;
using Bla.Domain.Users;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace Bla.Application.Tests.Users;

/// <summary>
/// Use-case behavior for <see cref="AuthService"/> with all ports mocked (NSubstitute):
/// happy-path register/login, duplicate-email rejection, generic bad-credentials, and validation.
/// </summary>
public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();

    private AuthService CreateSut() =>
        new(_users, _hasher, _tokens, new RegisterRequestValidator(), new LoginRequestValidator());

    [Fact]
    public async Task RegisterAsync_WithNewEmail_PersistsUserAndReturnsToken()
    {
        _users.ExistsByEmailAsync("new@bla.local", Arg.Any<CancellationToken>())
            .Returns(false);
        _hasher.Hash(Arg.Any<User>(), "Password1").Returns("hashed");
        var expiry = DateTime.UtcNow.AddHours(1);
        _tokens.CreateToken(Arg.Any<User>()).Returns(("the-token", expiry));

        var result = await CreateSut().RegisterAsync(new RegisterRequest("New@BLA.local", "Password1"));

        result.Token.Should().Be("the-token");
        result.ExpiresAtUtc.Should().Be(expiry);
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "new@bla.local" && u.PasswordHash == "hashed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsEmailAlreadyInUse()
    {
        _users.ExistsByEmailAsync("taken@bla.local", Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => CreateSut().RegisterAsync(new RegisterRequest("taken@bla.local", "Password1"));

        await act.Should().ThrowAsync<EmailAlreadyInUseException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidInput_ThrowsValidationException()
    {
        var act = () => CreateSut().RegisterAsync(new RegisterRequest("not-an-email", "short"));

        await act.Should().ThrowAsync<ValidationException>();
        await _users.DidNotReceive().ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = User.Create(Guid.NewGuid(), "user@bla.local", "stored-hash", DateTime.UtcNow);
        _users.GetByEmailAsync("user@bla.local", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(user, "stored-hash", "Password1").Returns(true);
        var expiry = DateTime.UtcNow.AddHours(1);
        _tokens.CreateToken(user).Returns(("tok", expiry));

        var result = await CreateSut().LoginAsync(new LoginRequest("User@BLA.local", "Password1"));

        result.Token.Should().Be("tok");
        result.ExpiresAtUtc.Should().Be(expiry);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync("ghost@bla.local", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateSut().LoginAsync(new LoginRequest("ghost@bla.local", "Password1"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        var user = User.Create(Guid.NewGuid(), "user@bla.local", "stored-hash", DateTime.UtcNow);
        _users.GetByEmailAsync("user@bla.local", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(user, "stored-hash", "WrongPass1").Returns(false);

        var act = () => CreateSut().LoginAsync(new LoginRequest("user@bla.local", "WrongPass1"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokens.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenFound_ReturnsResponseWithoutHash()
    {
        var user = User.Create(Guid.NewGuid(), "user@bla.local", "secret-hash", DateTime.UtcNow);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().GetCurrentUserAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("user@bla.local");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenMissing_ReturnsNull()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().GetCurrentUserAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
