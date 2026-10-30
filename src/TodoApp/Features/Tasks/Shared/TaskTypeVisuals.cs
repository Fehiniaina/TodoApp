using System.Collections.Frozen;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

/// <summary>
/// Apparence de chaque type (classe CSS). Explicite par type : pas de ToString().ToLower(),
/// qui couplerait le CSS au nom du membre (renommage = icône perdue).
/// Exhaustivité vérifiée par TaskTypeVisualsTests.
/// </summary>
internal static class TaskTypeVisuals
{
    public const string FallbackCss = "type-icon--unknown";

    private static readonly FrozenDictionary<TaskType, string> CssByType =
        new Dictionary<TaskType, string>
        {
            [TaskType.Bug] = "type-icon--bug",
            [TaskType.Extraction] = "type-icon--extraction",
            [TaskType.Analyse] = "type-icon--analysis",
            [TaskType.Task] = "type-icon--task",
            [TaskType.UserStory] = "type-icon--story",
        }.ToFrozenDictionary();

    public static string CssFor(TaskType type) =>
        CssByType.TryGetValue(type, out var css) ? css : FallbackCss;

    internal static bool HasVisual(TaskType type) => CssByType.ContainsKey(type);
}
