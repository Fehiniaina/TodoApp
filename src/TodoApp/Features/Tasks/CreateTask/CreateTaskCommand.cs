using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.CreateTask;

internal sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate = null,
    TaskType Type = TaskType.Task);
