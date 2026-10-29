using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

internal sealed record TaskTypeChanged(
    Guid TaskId,
    string ActorId,
    TaskType Previous,
    TaskType Current,
    DateTime OccurredAtUtc) : IDomainEvent;
