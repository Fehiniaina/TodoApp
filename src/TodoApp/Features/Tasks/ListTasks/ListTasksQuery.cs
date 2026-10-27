namespace TodoApp.Features.Tasks.ListTasks;

internal sealed record ListTasksQuery(TaskScope Scope, int Page, int PageSize)
{
    public TaskFilter Filter { get; init; } = TaskFilter.None;

    /// <summary>Date du jour dans le fuseau de l'utilisateur (filtres d'échéance).</summary>
    public DateOnly Today { get; init; }
}
