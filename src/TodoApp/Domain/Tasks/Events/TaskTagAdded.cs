using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Events;

/// <summary>TagName : le nom AU MOMENT de l'action (l'historique reste fidèle au passé).</summary>
internal sealed record TaskTagAdded(
    Guid TaskId, string ActorId, Guid TagId, string TagName, DateTime OccurredAtUtc) : IDomainEvent;
