namespace TodoApp.Domain.Tasks.Activity;

/// <summary>Ligne d'historique : ajout seul, jamais modifiée.</summary>
internal sealed class TaskActivity
{
    private TaskActivity() { }   // EF Core

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;   // sert au filtre de corbeille

    /// <summary>null = action du système (ex. occurrence de récurrence).</summary>
    public string? ActorId { get; private set; }

    public TaskActivityType Type { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static TaskActivity Record(
        Guid taskId, string? actorId, TaskActivityType type,
        string? oldValue, string? newValue, DateTime occurredAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TaskId = taskId,
            ActorId = actorId,
            Type = type,
            OldValue = oldValue,
            NewValue = newValue,
            OccurredAtUtc = occurredAtUtc,
        };
}
