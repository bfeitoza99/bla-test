using System.Collections.Concurrent;
using Bla.Application.Common;
using Bla.Application.Tasks;
using Bla.Domain.Tasks;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Api.Tests.Tasks;

/// <summary>
/// In-memory <see cref="ITaskRepository"/> for endpoint tests, so the create -> list -> get ->
/// update -> delete flow runs deterministically without a real PostgreSQL. Mirrors the production
/// repository's ownership scoping (every read/write is filtered by user id), due-date ordering, and
/// optional status filter so the tests exercise the same contract.
/// </summary>
public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _byId = new();

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _byId[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var task);
        // Ownership scoping: a task owned by someone else is invisible (indistinguishable from missing).
        return Task.FromResult(task is not null && task.UserId == userId ? task : null);
    }

    public Task<PagedResult<TaskItem>> ListByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        TaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values
            .Where(t => t.UserId == userId)
            .Where(t => status is null || t.Status == status)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .ToList();

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<TaskItem>(items, page, pageSize, query.Count));
    }

    public Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        if (_byId.TryGetValue(task.Id, out var existing) && existing.UserId == task.UserId)
        {
            _byId[task.Id] = task;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        if (_byId.TryGetValue(id, out var existing) && existing.UserId == userId)
        {
            return Task.FromResult(_byId.TryRemove(id, out _));
        }

        return Task.FromResult(false);
    }
}
