using Bla.Application.Tasks;
using FluentValidation.TestHelper;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tests.Tasks;

public class TaskListQueryValidatorTests
{
    private readonly TaskListQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_Passes()
    {
        var result = _validator.TestValidate(new TaskListQuery(1, 20, TaskStatus.Done));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullStatus_Passes()
    {
        var result = _validator.TestValidate(new TaskListQuery(1, 20, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageBelowOne_Fails(int page)
    {
        var result = _validator.TestValidate(new TaskListQuery(page, 20, null));

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_Fails(int pageSize)
    {
        var result = _validator.TestValidate(new TaskListQuery(1, pageSize, null));

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WithUndefinedStatus_Fails()
    {
        var result = _validator.TestValidate(new TaskListQuery(1, 20, (TaskStatus)99));

        result.ShouldHaveValidationErrorFor("Status.Value");
    }
}
