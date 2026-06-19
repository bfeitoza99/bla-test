using FluentValidation;

namespace Bla.Application.Users;

/// <summary>
/// Validates a <see cref="RegisterRequest"/>: a well-formed email and a password meeting the
/// minimum policy (length plus character classes). The server is authoritative on these rules.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Minimum password length enforced on registration.</summary>
    public const int MinPasswordLength = 8;

    /// <summary>Maximum password length (guards against absurdly long inputs / DoS via hashing).</summary>
    public const int MaxPasswordLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must be at most 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinPasswordLength)
                .WithMessage($"Password must be at least {MinPasswordLength} characters.")
            .MaximumLength(MaxPasswordLength)
                .WithMessage($"Password must be at most {MaxPasswordLength} characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an upper-case letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lower-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}
