using System.Globalization;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

public static class TaskTypeExtensions
{
    public static string ToLabel(this TaskType type) => type switch
    {
        TaskType.Bug => "Bug",
        TaskType.Extraction => "Extraction",
        TaskType.UserStory => "User story",
        TaskType.Analyse => "Analyse",
        TaskType.Task => "Tâche",
        _ => type.ToString(),
    };

    private static readonly StringComparer FrenchLabelComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true);

    /// <summary>
    /// Types triés par libellé (listes déroulantes, filtres). Indépendant des valeurs persistées.
    /// </summary>
    public static IReadOnlyList<TaskType> InDisplayOrder { get; } =
        [.. Enum.GetValues<TaskType>().OrderBy(t => t.ToLabel(), FrenchLabelComparer)];
}
