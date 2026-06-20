using Bla.Domain.Tasks;
using FluentAssertions;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Domain.Tests;

/// <summary>
/// Invariants for the <see cref="TaskItem"/> entity: identity, owner, a non-empty (trimmed) title,
/// a defined status, UTC timestamps, and an <see cref="TaskItem.Update"/> that bumps
/// <c>UpdatedAt</c>. No I/O is involved.
/// </summary>
public class TaskItemTests
{
    private static DateTime Utc(int day = 1) =>
        DateTime.SpecifyKind(new DateTime(2026, 1, day, 0, 0, 0), DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = Utc(10);
        var created = Utc(1);

        var task = TaskItem.Create(
            id, "Write report", "Quarterly numbers", TaskStatus.InProgress, due, userId, created, created);

        task.Id.Should().Be(id);
        task.Title.Should().Be("Write report");
        task.Description.Should().Be("Quarterly numbers");
        task.Status.Should().Be(TaskStatus.InProgress);
        task.DueDate.Should().Be(due);
        task.UserId.Should().Be(userId);
        task.CreatedAt.Should().Be(created);
        task.UpdatedAt.Should().Be(created);
    }

    [Fact]
    public void Create_TrimsTitleAndDescription()
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "  Trim me  ", "  desc  ", TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        task.Title.Should().Be("Trim me");
        task.Description.Should().Be("desc");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_Throws(string? title)
    {
        var act = () => TaskItem.Create(
            Guid.NewGuid(), title!, null, TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankDescription_StoresNull(string? description)
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "Title", description, TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        task.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        var act = () => TaskItem.Create(
            Guid.Empty, "Title", null, TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => TaskItem.Create(
            Guid.NewGuid(), "Title", null, TaskStatus.Todo, Utc(10), Guid.Empty, Utc(1), Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithUndefinedStatus_Throws()
    {
        var act = () => TaskItem.Create(
            Guid.NewGuid(), "Title", null, (TaskStatus)99, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonUtcDueDate_Throws()
    {
        var local = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Local);

        var act = () => TaskItem.Create(
            Guid.NewGuid(), "Title", null, TaskStatus.Todo, local, Guid.NewGuid(), Utc(1), Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonUtcCreatedAt_Throws()
    {
        var local = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);

        var act = () => TaskItem.Create(
            Guid.NewGuid(), "Title", null, TaskStatus.Todo, Utc(10), Guid.NewGuid(), local, Utc(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesMutableFieldsAndBumpsUpdatedAt()
    {
        var created = Utc(1);
        var task = TaskItem.Create(
            Guid.NewGuid(), "Old", "old desc", TaskStatus.Todo, Utc(10), Guid.NewGuid(), created, created);

        var laterUpdate = Utc(5);
        task.Update("New", "new desc", TaskStatus.Done, Utc(20), laterUpdate);

        task.Title.Should().Be("New");
        task.Description.Should().Be("new desc");
        task.Status.Should().Be(TaskStatus.Done);
        task.DueDate.Should().Be(Utc(20));
        task.UpdatedAt.Should().Be(laterUpdate);
        task.CreatedAt.Should().Be(created, "CreatedAt is immutable");
    }

    [Fact]
    public void Update_WithMissingTitle_Throws()
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "Title", null, TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));

        var act = () => task.Update("  ", null, TaskStatus.Done, Utc(20), Utc(5));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNonUtcUpdatedAt_Throws()
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "Title", null, TaskStatus.Todo, Utc(10), Guid.NewGuid(), Utc(1), Utc(1));
        var local = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Local);

        var act = () => task.Update("New", null, TaskStatus.Done, Utc(20), local);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Restore_RehydratesWithoutNormalizing()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var task = TaskItem.Restore(
            id, "  kept as-is  ", "desc", TaskStatus.Done, Utc(10), userId, Utc(1), Utc(2));

        task.Id.Should().Be(id);
        task.Title.Should().Be("  kept as-is  ");
        task.UserId.Should().Be(userId);
        task.UpdatedAt.Should().Be(Utc(2));
    }
}
