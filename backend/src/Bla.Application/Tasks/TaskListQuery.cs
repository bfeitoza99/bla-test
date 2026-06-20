using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tasks;

/// <summary>
/// Query parameters for listing a user's tasks: the page (1-based), the page size, and an optional
/// <see cref="Status"/> filter. Bound from the query string; defaults applied by the controller.
/// </summary>
public sealed record TaskListQuery(int Page, int PageSize, TaskStatus? Status);
