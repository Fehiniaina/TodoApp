using System.Diagnostics;
using Humanizer;
using TodoApp.Domain.Common;
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
                => $"{a.ActorName} a déplacé la tâche de « {Label<TaskItemStatus>(a.OldValue, t => t.ToLabel())} » à « {Label<TaskItemStatus>(a.NewValue, t => t.ToLabel())} »",
            TaskActivityType.AttachmentAdded => $"{a.ActorName} a ajouté la pièce jointe « {a.NewValue} »",
            TaskActivityType.AttachmentRemoved => $"{a.ActorName} a supprimé la pièce jointe « {a.OldValue} »",
            TaskActivityType.TypeChanged =>
                $"{a.ActorName} a changé le type de {Label<TaskType>(a.OldValue, t => t.ToLabel())} à {Label<TaskType>(a.NewValue, t => t.ToLabel())}",
            TaskActivityType.TagAdded => $"{a.ActorName} a ajouté le tag « {a.NewValue} »",
            TaskActivityType.TagRemoved => $"{a.ActorName} a retiré le tag « {a.OldValue} »",
            _ => $"{a.ActorName} a modifié la tâche",
        };
    }

    /// <summary>
    /// Libellé d'une valeur d'enum stockée. Jamais d'exception : une valeur illisible
    /// (ligne non migrée, valeur inconnue) s'affiche brute plutôt que de couper le circuit.
    /// </summary>
    private static string Label<TEnum>(string? stored, Func<TEnum, string> toLabel)
        where TEnum : struct, Enum =>
        EnumStorage.TryFromStored<TEnum>(stored, out var value) ? toLabel(value) : stored ?? "?";
}
