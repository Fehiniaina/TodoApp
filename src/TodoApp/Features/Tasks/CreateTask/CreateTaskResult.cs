using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.CreateTask;

internal sealed record CreateTaskResult(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime CreatedAtUtc);