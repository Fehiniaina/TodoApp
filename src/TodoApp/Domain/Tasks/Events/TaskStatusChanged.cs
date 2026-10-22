using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

internal sealed record TaskStatusChanged(
    Guid TaskId,
    string ActorId,
    TaskItemStatus From,
    TaskItemStatus To,
    DateTime OccurredAtUtc) : IDomainEvent;
