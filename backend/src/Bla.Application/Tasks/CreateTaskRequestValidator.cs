using FluentValidation;

namespace Bla.Application.Tasks;

/// <summary>
/// Validates a <see cref="CreateTaskRequest"/>: a required, length-bounded title, a bounded optional
/// description, a known <c>Status</c> enum value, and a sane (not absurdly far past/future) due
/// date. The server is authoritative on these rules.
/// </summary>
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskValidation.MaxTitleLength)
                .WithMessage($"Title must be at most {TaskValidation.MaxTitleLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(TaskValidation.MaxDescriptionLength)
                .WithMessage($"Description must be at most {TaskValidation.MaxDescriptionLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a known value.");

        RuleFor(x => x.DueDate)
            .Must(TaskValidation.IsSaneDueDate)
                .WithMessage("DueDate is outside the supported range.");
    }
}
