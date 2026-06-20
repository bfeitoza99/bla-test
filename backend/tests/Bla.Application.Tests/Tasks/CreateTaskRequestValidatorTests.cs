using Bla.Application.Tasks;
using FluentValidation.TestHelper;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Application.Tests.Tasks;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    private static DateTime SaneDue() => DateTime.UtcNow.AddDays(7);

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var result = _validator.TestValidate(
            new CreateTaskRequest("Write report", "Some detail", TaskStatus.Todo, SaneDue()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullDescription_Passes()
    {
        var result = _validator.TestValidate(
            new CreateTaskRequest("Title", null, TaskStatus.Todo, SaneDue()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingTitle_Fails(string title)
    {
        var result = _validator.TestValidate(
            new CreateTaskRequest(title, null, TaskStatus.Todo, SaneDue()));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithTooLongTitle_Fails()
    {
        var title = new string('x', TaskValidation.MaxTitleLength + 1);

        var result = _validator.TestValidate(
            new CreateTaskRequest(title, null, TaskStatus.Todo, SaneDue()));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WithTooLongDescription_Fails()
    {
        var description = new string('x', TaskValidation.MaxDescriptionLength + 1);

        var result = _validator.TestValidate(
            new CreateTaskRequest("Title", description, TaskStatus.Todo, SaneDue()));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithUndefinedStatus_Fails()
    {
        var result = _validator.TestValidate(
            new CreateTaskRequest("Title", null, (TaskStatus)99, SaneDue()));

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_WithBogusDueDate_Fails()
    {
        var ancient = DateTime.SpecifyKind(new DateTime(1900, 1, 1), DateTimeKind.Utc);

        var result = _validator.TestValidate(
            new CreateTaskRequest("Title", null, TaskStatus.Todo, ancient));

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }
}
