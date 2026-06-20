using Bla.Application.Common;
using Bla.Domain.Tasks;

namespace Bla.Application.Tasks;

/// <summary>
/// Hand-written mapping from the <see cref="TaskItem"/> domain entity to its HTTP-facing DTO.
/// Deliberately omits the owner id — the caller is always the owner, so it adds nothing and is kept
/// off the wire.
/// </summary>
public static class TaskMappings
{
    public static TaskResponse ToResponse(this TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt);

    public static PagedResult<TaskResponse> ToResponse(this PagedResult<TaskItem> page) =>
        new(
            page.Items.Select(t => t.ToResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.Total);
}
