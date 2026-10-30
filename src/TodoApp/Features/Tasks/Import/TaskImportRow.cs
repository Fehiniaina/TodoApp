using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Import;

internal sealed record TaskImportRow(
    int LineNumber,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    string? AssigneeEmail,
    string? AssigneeId,
    string? AssigneeName,
    TaskType TaskType,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
