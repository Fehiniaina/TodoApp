using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.UpdateTask;

internal sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskType Type,
    DateOnly? DueDate = null);
