using Bla.Application.Users;
using FluentValidation.TestHelper;

namespace Bla.Application.Tests.Users;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var result = _validator.TestValidate(new RegisterRequest("person@example.com", "Password1"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("two@@at.com")]
    public void Validate_WithBadEmail_Fails(string email)
    {
        var result = _validator.TestValidate(new RegisterRequest(email, "Password1"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1A")]      // < 8 chars
    [InlineData("password1")]    // no upper-case
    [InlineData("PASSWORD1")]    // no lower-case
    [InlineData("PasswordOnly")] // no digit
    public void Validate_WithWeakPassword_Fails(string password)
    {
        var result = _validator.TestValidate(new RegisterRequest("person@example.com", password));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
