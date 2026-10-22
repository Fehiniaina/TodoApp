using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

internal sealed record TaskCreated(Guid TaskId, string? ActorId, DateTime OccurredAtUtc) : IDomainEvent;
