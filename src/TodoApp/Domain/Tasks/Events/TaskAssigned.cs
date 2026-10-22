using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

internal sealed record TaskAssigned(
    Guid TaskId,
    string ActorId,
    string? PreviousAssigneeId,
    string? NewAssigneeId,
    DateTime OccurredAtUtc) : IDomainEvent;
