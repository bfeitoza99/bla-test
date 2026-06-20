using Bla.Application.Common;
using Bla.Domain.Tasks;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tasks;

/// <summary>
/// Persistence port for <see cref="TaskItem"/> aggregates. Implemented in Infrastructure with
/// hand-written, parameterized SQL. Every read/write is scoped to the owning user so a caller can
/// only ever see or change their own tasks — ownership is enforced in the SQL, not just the service.
/// </summary>
public interface ITaskRepository
{
    /// <summary>Persists a new task.</summary>
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the task with the given id <em>if it belongs to the given user</em>, otherwise
    /// <see langword="null"/> (the not-owned and not-found cases are indistinguishable on purpose).
    /// </summary>
    Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single page of the user's tasks, ordered by due date, optionally filtered by
    /// status, with the total count across all pages for paging metadata.
    /// </summary>
    Task<PagedResult<TaskItem>> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        TaskStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing task scoped to its owner. Returns <see langword="true"/> when
    /// a row was updated, <see langword="false"/> when no owned task with that id exists.
    /// </summary>
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the user's task. Returns <see langword="true"/> when a row was removed,
    /// <see langword="false"/> when no owned task with that id exists.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
