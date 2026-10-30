using TodoApp.Domain.Tasks;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

/// <summary>
/// Fabrique de tâches pour les tests de specs. Un seul endroit dépend de la signature
/// de TaskItem.Create : une évolution du domaine ne casse pas tous les fichiers de tests.
/// </summary>
internal static class TestTasks
{
    public const string Creator = "creator-id";

    public static readonly DateTime CreatedAtUtc = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    public static TaskItem Create(
        TaskPriority priority = TaskPriority.Medium,
        TaskType type = TaskType.Task,
        DateOnly? dueDate = null,
        string? assigneeId = null,
        TaskItemStatus status = TaskItemStatus.ToDo)
    {
        var task = TaskItem.Create(Creator, "Tâche de test", null, priority, dueDate, CreatedAtUtc, type);

        if (assigneeId is not null)
        {
            task.AssignTo(assigneeId, Creator, CreatedAtUtc);
        }

        foreach (var step in PathTo(status))
        {
            task.ChangeStatus(step, Creator, CreatedAtUtc);
        }

        return task;
    }

    /// <summary>
    /// Plus court chemin autorisé par le workflow depuis « À faire » :
    /// les tests restent valides si les transitions évoluent.
    /// </summary>
    private static Stack<TaskItemStatus> PathTo(TaskItemStatus target)
    {
        var previous = new Dictionary<TaskItemStatus, TaskItemStatus>();
        var visited = new HashSet<TaskItemStatus> { TaskItemStatus.ToDo };
        var queue = new Queue<TaskItemStatus>();
        queue.Enqueue(TaskItemStatus.ToDo);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                break;
            }

            foreach (var next in TaskWorkflow.NextStatuses(current))
            {
                if (visited.Add(next))
                {
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (!visited.Contains(target))
        {
            throw new InvalidOperationException($"Statut {target} inaccessible depuis ToDo dans le workflow.");
        }

        var path = new Stack<TaskItemStatus>();
        for (var status = target; status != TaskItemStatus.ToDo; status = previous[status])
        {
            path.Push(status);
        }

        return path;
    }
}
