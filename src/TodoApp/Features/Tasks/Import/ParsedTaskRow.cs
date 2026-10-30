using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Import;

internal sealed record ParsedTaskRow(
    int LineNumber,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    string? AssigneeEmail,
    TaskType Type,
    IReadOnlyList<string> Errors);
