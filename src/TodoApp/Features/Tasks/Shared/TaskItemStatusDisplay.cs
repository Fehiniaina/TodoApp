using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public static class TaskItemStatusDisplay
{
    public static string ToLabel(this TaskItemStatus status) => status switch
    {
        TaskItemStatus.ToDo => "À faire",
        TaskItemStatus.InProgress => "En cours",
        TaskItemStatus.InValidation => "En validation",
        TaskItemStatus.Resolved => "Résolue",
        _ => status.ToString(),
    };

    public static string ToLozengeCss(this TaskItemStatus status) => status switch
    {
        TaskItemStatus.InProgress => "lozenge-progress",
        TaskItemStatus.InValidation => "lozenge-review",
        TaskItemStatus.Resolved => "lozenge-done",
        _ => "lozenge-todo",
    };
}
