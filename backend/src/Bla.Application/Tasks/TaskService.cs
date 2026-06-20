using Bla.Application.Common;
using Bla.Domain.Tasks;
using FluentValidation;

namespace Bla.Application.Tasks;

/// <summary>
/// Orchestrates the task CRUD use cases over the domain and the repository port. Holds no
/// infrastructure knowledge. Ownership is enforced twice over: the service passes the authenticated
/// user id into every repository call, and the repository scopes every query by it.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<TaskListQuery> _listValidator;

    public TaskService(
        ITaskRepository tasks,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskListQuery> listValidator)
    {
        _tasks = tasks;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _listValidator = listValidator;
    }

    public async Task<TaskResponse> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = DateTime.UtcNow;
        var task = TaskItem.Create(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Status,
            EnsureUtc(request.DueDate),
            userId,
            now,
            now);

        await _tasks.AddAsync(task, cancellationToken);

        return task.ToResponse();
    }

    public async Task<TaskResponse> GetAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(id, userId, cancellationToken);
        if (task is null)
        {
            throw new TaskNotFoundException();
        }

        return task.ToResponse();
    }

    public async Task<PagedResult<TaskResponse>> ListAsync(
        Guid userId,
        TaskListQuery query,
        CancellationToken cancellationToken = default)
    {
        await _listValidator.ValidateAndThrowAsync(query, cancellationToken);

        var page = await _tasks.ListByUserAsync(
            userId,
            query.Page,
            query.PageSize,
            query.Status,
            cancellationToken);

        return page.ToResponse();
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid userId,
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Load scoped to the owner first — a missing/not-owned task is a 404, never a silent no-op.
        var task = await _tasks.GetByIdAsync(id, userId, cancellationToken);
        if (task is null)
        {
            throw new TaskNotFoundException();
        }

        task.Update(
            request.Title,
            request.Description,
            request.Status,
            EnsureUtc(request.DueDate),
            DateTime.UtcNow);

        var updated = await _tasks.UpdateAsync(task, cancellationToken);
        if (!updated)
        {
            // Lost the row between the read and the write (e.g. concurrent delete): still a 404.
            throw new TaskNotFoundException();
        }

        return task.ToResponse();
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _tasks.DeleteAsync(id, userId, cancellationToken);
        if (!deleted)
        {
            throw new TaskNotFoundException();
        }
    }

    /// <summary>
    /// Coerces an incoming due date to UTC so the domain's UTC invariant holds. An
    /// <see cref="DateTimeKind.Unspecified"/> value (common from JSON without an offset) is treated
    /// as already UTC; a local value is converted.
    /// </summary>
    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
