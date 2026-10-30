using System.Collections.Frozen;

namespace TodoApp.Domain.Tasks;

internal static class TaskWorkflow
{
    private static readonly FrozenDictionary<TaskItemStatus, FrozenSet<TaskItemStatus>> Allowed =
        new Dictionary<TaskItemStatus, FrozenSet<TaskItemStatus>>
        {
            [TaskItemStatus.ToDo] = Next(TaskItemStatus.InProgress),
            [TaskItemStatus.InProgress] = Next(TaskItemStatus.InValidation, TaskItemStatus.ToDo),
            [TaskItemStatus.InValidation] = Next(TaskItemStatus.Resolved, TaskItemStatus.InProgress),
            [TaskItemStatus.Resolved] = Next(TaskItemStatus.InProgress),
        }.ToFrozenDictionary();

    public static bool CanMove(TaskItemStatus from, TaskItemStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>Transitions possibles depuis un statut : sert aussi à alimenter le menu de l'interface.</summary>
    public static IReadOnlySet<TaskItemStatus> NextStatuses(TaskItemStatus from) =>
        Allowed.TryGetValue(from, out var next) ? next : FrozenSet<TaskItemStatus>.Empty;

    private static FrozenSet<TaskItemStatus> Next(params TaskItemStatus[] statuses) => statuses.ToFrozenSet();

    /// <summary>Statuts considérés comme terminés : ni en retard, ni à traiter.</summary>
    public static readonly TaskItemStatus[] ClosedStatuses = [TaskItemStatus.Resolved, TaskItemStatus.Done];
}
