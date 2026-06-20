using Bla.Application.Common;

namespace Bla.Application.Tasks;

/// <summary>
/// Use-case surface for the Tasks slice. Every operation is scoped to the authenticated user id
/// (supplied by the API from the validated token, never from the request body): a caller can only
/// see or change their own tasks.
/// </summary>
public interface ITaskService
{
    /// <summary>Creates a task for the user and returns its DTO.</summary>
    Task<TaskResponse> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user's task by id.
    /// </summary>
    /// <exception cref="TaskNotFoundException">When no task with that id is owned by the user.</exception>
    Task<TaskResponse> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of the user's tasks, optionally filtered by status.</summary>
    Task<PagedResult<TaskResponse>> ListAsync(
        Guid userId,
        TaskListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an update to the user's task and returns the new DTO.
    /// </summary>
    /// <exception cref="TaskNotFoundException">When no task with that id is owned by the user.</exception>
    Task<TaskResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the user's task.
    /// </summary>
    /// <exception cref="TaskNotFoundException">When no task with that id is owned by the user.</exception>
    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
