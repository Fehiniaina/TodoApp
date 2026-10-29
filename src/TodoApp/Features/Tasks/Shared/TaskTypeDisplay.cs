using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public static class TaskTypeDisplay
{
    public static string ToLabel(this TaskType type) => type switch
    {
        TaskType.Bug => "Bug",
        TaskType.Extracte => "Extracte",
        TaskType.Analyse => "Analyse",
        TaskType.Task => "Tâche",
        _ => type.ToString(),
    };
}
