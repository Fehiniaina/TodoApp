using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ChangeTaskStatus;

/// <summary>Ce que l'UI doit savoir pour mettre à jour la ligne sans recharger la liste.</summary>
public sealed record ChangeTaskStatusResult(
    Guid TaskId,
    TaskItemStatus Status,
    DateTime? CompletedAt,
    IReadOnlyCollection<TaskItemStatus> AllowedNextStatuses);
