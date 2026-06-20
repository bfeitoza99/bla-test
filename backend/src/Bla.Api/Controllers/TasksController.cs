using Bla.Api.Authentication;
using Bla.Application.Common;
using Bla.Application.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Api.Controllers;

/// <summary>
/// Tasks API: full CRUD over the authenticated user's own tasks. Every endpoint is authorized; the
/// owner id is read from the validated token via <see cref="ClaimsPrincipalExtensions.GetUserId"/>
/// and never from the request body or query. The controller is thin — it delegates to
/// <see cref="ITaskService"/> and translates the outcome (or a known use-case exception) into the
/// right HTTP status + ProblemDetails.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    /// <summary>Default page when the caller omits it.</summary>
    public const int DefaultPage = 1;

    /// <summary>Default page size when the caller omits it (max 100, enforced by the validator).</summary>
    public const int DefaultPageSize = 20;

    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>Lists the authenticated user's tasks, paginated and optionally filtered by status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] int page = DefaultPage,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] TaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _taskService.ListAsync(
                userId,
                new TaskListQuery(page, pageSize, status),
                cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>Returns one of the authenticated user's tasks by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _taskService.GetAsync(userId, id, cancellationToken);
            return Ok(result);
        }
        catch (TaskNotFoundException)
        {
            return NotFoundProblem();
        }
    }

    /// <summary>Creates a task for the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _taskService.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>Updates one of the authenticated user's tasks.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        try
        {
            var result = await _taskService.UpdateAsync(userId, id, request, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ex);
        }
        catch (TaskNotFoundException)
        {
            return NotFoundProblem();
        }
    }

    /// <summary>Deletes one of the authenticated user's tasks.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        try
        {
            await _taskService.DeleteAsync(userId, id, cancellationToken);
            return NoContent();
        }
        catch (TaskNotFoundException)
        {
            return NotFoundProblem();
        }
    }

    private ObjectResult NotFoundProblem() =>
        Problem(
            title: "Task not found.",
            detail: "The task does not exist or is not owned by the current user.",
            statusCode: StatusCodes.Status404NotFound);

    private ActionResult ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
        });
    }
}
