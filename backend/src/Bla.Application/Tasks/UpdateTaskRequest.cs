using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tasks;

/// <summary>Update-task request: the full new state of a task's editable fields.</summary>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskStatus Status,
    DateTime DueDate);
