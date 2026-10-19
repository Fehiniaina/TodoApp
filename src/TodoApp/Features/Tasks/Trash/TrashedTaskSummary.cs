using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Trash;

public sealed record TrashedTaskSummary(Guid Id, string Title, TaskPriority Priority, DateTime DeletedAt);