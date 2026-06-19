using Bla.Application.Users;
using FluentValidation.TestHelper;

namespace Bla.Application.Tests.Users;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var result = _validator.TestValidate(new LoginRequest("person@example.com", "anything"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        var result = _validator.TestValidate(new LoginRequest("", "anything"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var result = _validator.TestValidate(new LoginRequest("person@example.com", ""));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
