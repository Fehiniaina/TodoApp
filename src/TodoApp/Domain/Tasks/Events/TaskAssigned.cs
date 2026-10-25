using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

public sealed record TaskAssigned(
    Guid TaskId,
    string ActorId,
    string? PreviousAssigneeId,
    string? NewAssigneeId,
    DateTime OccurredAtUtc,
    AssignmentOrigin Origin = AssignmentOrigin.Manual) : IDomainEvent;
