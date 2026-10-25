using System.Diagnostics;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Activity;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Activity;

public static class TaskActivityFormatter
{
    public static string Describe(TaskActivityDto a)
    {
        ArgumentNullException.ThrowIfNull(a);

        return a.Type switch
        {
            TaskActivityType.Created when string.Equals(a.ActorName, "Système", StringComparison.Ordinal)
                => "Créée automatiquement (tâche récurrente)",
            TaskActivityType.Created
                => $"{a.ActorName} a créé la tâche",

            TaskActivityType.Assigned when a.NewValue is null
                => $"{a.ActorName} a retiré l'assignation",
            TaskActivityType.Assigned when string.Equals(a.NewValue, a.ActorName, StringComparison.Ordinal)
                => $"{a.ActorName} s'est assigné la tâche",
            TaskActivityType.Assigned when a.OldValue is null
                => $"{a.ActorName} a assigné la tâche à {a.NewValue}",
            TaskActivityType.Assigned
                => $"{a.ActorName} a réassigné la tâche de {a.OldValue} à {a.NewValue}",

            TaskActivityType.StatusChanged
                => $"{a.ActorName} a déplacé la tâche de « {StatusLabel(a.OldValue)} » à « {StatusLabel(a.NewValue)} »",
            TaskActivityType.AttachmentAdded => $"{a.ActorName} a ajouté la pièce jointe « {a.NewValue} »",
            TaskActivityType.AttachmentRemoved => $"{a.ActorName} a supprimé la pièce jointe « {a.OldValue} »",

            _ => $"{a.ActorName} a modifié la tâche",
        };
    }

    private static string StatusLabel(string? value) =>
        Enum.TryParse<TaskItemStatus>(value, out var status) ? status.ToLabel() : value ?? "?";
}
