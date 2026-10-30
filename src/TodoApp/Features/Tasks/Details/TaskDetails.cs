using TodoApp.Domain.Tasks;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.Features.Tasks.Details;

internal sealed record TaskDetails(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTime CreatedAtUtc,
    DateTime? CompletedAt,
    Guid? RecurringTaskId,
    string CreatedById,
    string CreatedByName,
    string? AssigneeId,
    string? AssigneeName,
    int ChecklistDone,
    int ChecklistTotal,
    TaskType Type,
    IReadOnlyList<TagDto> Tags)
{
    public bool IsCompleted => CompletedAt is not null;

    /// <summary>Référence courte affichée dans le fil d'Ariane.</summary>
    public string Key => $"TASK-{Id.ToString("N")[..8].ToUpperInvariant()}";
}
