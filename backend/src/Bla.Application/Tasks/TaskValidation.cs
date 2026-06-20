namespace Bla.Application.Tasks;

/// <summary>
/// Shared limits and sanity checks for task validation, so the create and update validators stay in
/// lock-step. Kept in one place as the single source of truth for these rules.
/// </summary>
public static class TaskValidation
{
    /// <summary>Maximum title length.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>Maximum description length.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>
    /// Lower bound for a due date: nothing predates the year 2000 (guards obviously bogus input
    /// without rejecting legitimately overdue tasks).
    /// </summary>
    public static readonly DateTime MinDueDate =
        DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);

    /// <summary>Upper bound for a due date: at most 100 years out.</summary>
    public static readonly DateTime MaxDueDate =
        DateTime.SpecifyKind(new DateTime(2100, 1, 1), DateTimeKind.Utc);

    /// <summary>Whether the due date falls within the supported range.</summary>
    public static bool IsSaneDueDate(DateTime dueDate)
    {
        var utc = dueDate.Kind == DateTimeKind.Utc ? dueDate : dueDate.ToUniversalTime();
        return utc >= MinDueDate && utc <= MaxDueDate;
    }
}
