using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

internal sealed record TaskTagRemoved(
    Guid TaskId, string ActorId, Guid TagId, string TagName, DateTime OccurredAtUtc) : IDomainEvent;
