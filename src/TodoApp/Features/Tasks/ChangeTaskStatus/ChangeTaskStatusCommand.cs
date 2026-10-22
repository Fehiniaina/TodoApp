using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ChangeTaskStatus;

internal sealed record ChangeTaskStatusCommand(Guid TaskId, TaskItemStatus NewStatus);
