using Bla.Application.Common;
using Bla.Application.Tasks;
using Bla.Domain.Tasks;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tests.Tasks;

/// <summary>
/// Use-case behavior for <see cref="TaskService"/> with the repository mocked (NSubstitute):
/// create, get/update/delete ownership (404 when not owned), and list pagination + status filter.
/// Real validators are used so invalid input surfaces as a <see cref="ValidationException"/>.
/// </summary>
public class TaskServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();

    private TaskService CreateSut() =>
        new(
            _tasks,
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new TaskListQueryValidator());

    private static DateTime SaneDue() => DateTime.UtcNow.AddDays(3);

    private static TaskItem ExistingTask(Guid id, Guid userId) =>
        TaskItem.Create(
            id, "Existing", "desc", TaskStatus.Todo, SaneDue(), userId, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task CreateAsync_PersistsTaskForUserAndReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var request = new CreateTaskRequest("New task", "detail", TaskStatus.InProgress, SaneDue());

        var result = await CreateSut().CreateAsync(userId, request);

        result.Title.Should().Be("New task");
        result.Status.Should().Be(TaskStatus.InProgress);
        result.Id.Should().NotBe(Guid.Empty);
        await _tasks.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.UserId == userId && t.Title == "New task"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidInput_ThrowsAndDoesNotPersist()
    {
        var act = () => CreateSut().CreateAsync(
            Guid.NewGuid(),
            new CreateTaskRequest("", null, TaskStatus.Todo, SaneDue()));

        await act.Should().ThrowAsync<ValidationException>();
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenOwned_ReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _tasks.GetByIdAsync(id, userId, Arg.Any<CancellationToken>())
            .Returns(ExistingTask(id, userId));

        var result = await CreateSut().GetAsync(userId, id);

        result.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetAsync_WhenNotOwnedOrMissing_Throws404()
    {
        _tasks.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var act = () => CreateSut().GetAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<TaskNotFoundException>();
    }

    [Fact]
    public async Task ListAsync_ReturnsPagedResponseAndForwardsFilter()
    {
        var userId = Guid.NewGuid();
        var items = new List<TaskItem> { ExistingTask(Guid.NewGuid(), userId) };
        _tasks.ListByUserAsync(userId, 2, 10, TaskStatus.Done, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskItem>(items, 2, 10, 11));

        var result = await CreateSut().ListAsync(userId, new TaskListQuery(2, 10, TaskStatus.Done));

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Total.Should().Be(11);
        result.Items.Should().HaveCount(1);
        await _tasks.Received(1).ListByUserAsync(
            userId, 2, 10, TaskStatus.Done, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_WithInvalidPaging_Throws()
    {
        var act = () => CreateSut().ListAsync(Guid.NewGuid(), new TaskListQuery(0, 20, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenOwned_UpdatesAndReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _tasks.GetByIdAsync(id, userId, Arg.Any<CancellationToken>())
            .Returns(ExistingTask(id, userId));
        _tasks.UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().UpdateAsync(
            userId, id, new UpdateTaskRequest("Updated", "new", TaskStatus.Done, SaneDue()));

        result.Title.Should().Be("Updated");
        result.Status.Should().Be(TaskStatus.Done);
        await _tasks.Received(1).UpdateAsync(
            Arg.Is<TaskItem>(t => t.Title == "Updated"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwned_Throws404AndDoesNotWrite()
    {
        _tasks.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var act = () => CreateSut().UpdateAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new UpdateTaskRequest("Updated", null, TaskStatus.Done, SaneDue()));

        await act.Should().ThrowAsync<TaskNotFoundException>();
        await _tasks.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenOwned_Deletes()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _tasks.DeleteAsync(id, userId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateSut().DeleteAsync(userId, id);

        await _tasks.Received(1).DeleteAsync(id, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwned_Throws404()
    {
        _tasks.DeleteAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => CreateSut().DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<TaskNotFoundException>();
    }
}
