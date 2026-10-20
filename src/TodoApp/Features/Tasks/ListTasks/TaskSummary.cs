using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

public sealed record TaskSummary(
    Guid Id, string Title, string? Description, TaskPriority Priority,
    DateTime CreatedAtUtc, TaskItemStatus Status, DateTime? CompletedAt, DateOnly? DueDate,
    int ChecklistDone, int ChecklistTotal, Guid? RecurringTaskId)
{
    public bool IsCompleted => Status == TaskItemStatus.Resolved;
}
