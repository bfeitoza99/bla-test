using FluentValidation;

namespace Bla.Application.Tasks;

/// <summary>
/// Validates a <see cref="TaskListQuery"/>: a 1-based page, a page size within
/// <c>[1, <see cref="MaxPageSize"/>]</c>, and (when supplied) a known <c>Status</c> enum value.
/// </summary>
public sealed class TaskListQueryValidator : AbstractValidator<TaskListQuery>
{
    /// <summary>Largest page size a caller may request (caps the work per query).</summary>
    public const int MaxPageSize = 100;

    public TaskListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
                .WithMessage($"PageSize must be between 1 and {MaxPageSize}.");

        RuleFor(x => x.Status!.Value)
            .IsInEnum().WithMessage("Status must be a known value.")
            .When(x => x.Status is not null);
    }
}
