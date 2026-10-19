using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public static class TaskPriorityDisplay
{
    public static string ToLabel(this TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Basse",
        TaskPriority.Medium => "Moyenne",
        TaskPriority.High => "Haute",
        TaskPriority.Urgent => "Urgente",
        _ => priority.ToString(),
    };

    public static string ToBadgeCss(this TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "text-bg-secondary",
        TaskPriority.Medium => "text-bg-info",
        TaskPriority.High => "text-bg-warning",
        TaskPriority.Urgent => "text-bg-danger",
        _ => "text-bg-light",
    };
}
