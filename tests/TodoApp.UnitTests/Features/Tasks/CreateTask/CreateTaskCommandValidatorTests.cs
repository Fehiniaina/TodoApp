using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.CreateTask;

namespace TodoApp.UnitTests.Features.Tasks.CreateTask;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    private static CreateTaskCommand ValidCommand(
        string title = "Buy groceries",
        string? description = null,
        TaskPriority priority = TaskPriority.Medium) =>
        new("owner-1", title, description, priority);

    [Fact]
    public void Validate_EmptyTitle_ReturnsInvalid()
    {
        var result = _validator.Validate(ValidCommand(title: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_TitleWhitespaceOnly_ReturnsInvalid()
    {
        var result = _validator.Validate(ValidCommand(title: "   "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_TitleExceedsMaxLength_ReturnsInvalid()
    {
        var result = _validator.Validate(ValidCommand(title: new string('x', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact]
    public void Validate_TitleAtMaxLength_ReturnsValid()
    {
        var result = _validator.Validate(ValidCommand(title: new string('x', 200)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PriorityOutOfRange_ReturnsInvalid()
    {
        var result = _validator.Validate(ValidCommand(priority: (TaskPriority)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Priority));
    }

    [Fact]
    public void Validate_DescriptionNull_ReturnsValid()
    {
        var result = _validator.Validate(ValidCommand(description: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ReturnsInvalid()
    {
        var result = _validator.Validate(ValidCommand(description: new string('x', 2001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Description));
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_ReturnsValid()
    {
        var result = _validator.Validate(ValidCommand(description: new string('x', 2000)));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)] // Low
    [InlineData(1)] // Medium
    [InlineData(2)] // High
    public void Validate_ValidPriority_ReturnsValid(int priorityValue)
    {
        var priority = (TaskPriority)priorityValue;
        var result = _validator.Validate(ValidCommand(priority: priority));

        Assert.True(result.IsValid);
    }
}
