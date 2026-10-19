using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

public sealed record TaskSummary(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime CreatedAtUtc,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateOnly? DueDate);
