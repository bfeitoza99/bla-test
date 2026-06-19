using FluentValidation;

namespace Bla.Application.Users;

/// <summary>
/// Validates a <see cref="LoginRequest"/>. Only shape is checked here (required fields, sane
/// lengths); whether the credentials are correct is decided by the use case, which returns a
/// single generic error so the response never reveals which field was wrong.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must be at most 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(RegisterRequestValidator.MaxPasswordLength)
                .WithMessage(
                    $"Password must be at most {RegisterRequestValidator.MaxPasswordLength} characters.");
    }
}
