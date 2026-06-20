using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tasks;

/// <summary>Create-task request. The owner is taken from the token, never from the body.</summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskStatus Status,
    DateTime DueDate);
