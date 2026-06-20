using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tasks;

/// <summary>Public view of a task. Owner id is intentionally omitted — it never leaves the server.</summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
