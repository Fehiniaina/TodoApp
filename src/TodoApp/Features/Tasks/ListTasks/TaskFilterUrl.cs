using System.Collections.Frozen;
using System.Globalization;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>
/// Aller-retour <see cref="TaskFilter"/> ⇄ query string.
/// Lecture tolérante : toute valeur invalide est ignorée (une URL bricolée ne lève jamais d'exception).
/// Écriture canonique : ordre stable, pour que deux filtres égaux produisent la même URL.
/// </summary>
internal static class TaskFilterUrl
{
    public const string PriorityParam = "priority";
    public const string AssigneeParam = "assignee";
    public const string DueParam = "due";
    public const string DueFromParam = "from";
    public const string DueToParam = "to";
    public const string TypeParam = "type";

    /// <summary>Valeur spéciale de <c>assignee</c> pour « non assignée » (les ids Identity sont des GUID).</summary>
    public const string UnassignedValue = "none";

    private const string DateFormat = "yyyy-MM-dd";
    private const int MaxValuesPerParam = 50;
    private const int MaxIdLength = 64;

    // Slugs explicites : renommer un membre de l'enum ne casse pas les favoris.
    private static readonly FrozenDictionary<string, DueDateFilter> DueBySlug =
        new Dictionary<string, DueDateFilter>(StringComparer.OrdinalIgnoreCase)
        {
            ["overdue"] = DueDateFilter.Overdue,
            ["today"] = DueDateFilter.Today,
            ["next7days"] = DueDateFilter.Next7Days,
            ["none"] = DueDateFilter.NoDueDate,
            ["range"] = DueDateFilter.Range,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<DueDateFilter, string> SlugByDue =
        DueBySlug.ToFrozenDictionary(kv => kv.Value, kv => kv.Key);

    // Par nom uniquement : Enum.TryParse accepterait "42" et produirait une valeur non définie.
    private static readonly FrozenDictionary<string, TaskPriority> PriorityByName =
        Enum.GetValues<TaskPriority>()
            .ToFrozenDictionary(p => p.ToString(), StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, TaskType> TypeByName =
        Enum.GetValues<TaskType>()
            .ToFrozenDictionary(t => t.ToString(), StringComparer.OrdinalIgnoreCase);

    public static TaskFilter Parse(
        IEnumerable<string>? priorities,
        IEnumerable<string>? assignees,
        IEnumerable<string>? types,
        string? due,
        string? dueFrom,
        string? dueTo)
    {
        var parsedPriorities = new HashSet<TaskPriority>();
        foreach (var value in Bounded(priorities))
        {
            if (PriorityByName.TryGetValue(value, out var priority))
            {
                parsedPriorities.Add(priority);
            }
        }

        var assigneeIds = new HashSet<string>(StringComparer.Ordinal);
        var includeUnassigned = false;
        foreach (var value in Bounded(assignees))
        {
            if (string.Equals(value, UnassignedValue, StringComparison.OrdinalIgnoreCase))
            {
                includeUnassigned = true;
            }
            else if (value.Length <= MaxIdLength)
            {
                assigneeIds.Add(value);
            }
        }

        var parsedTypes = new HashSet<TaskType>();
        foreach(var value in Bounded(types))
        {
            if (TypeByName.TryGetValue(value, out var type))
            {
                parsedTypes.Add(type);
            }
        }

        var (mode, from, to) = ParseDueDate(due, dueFrom, dueTo);

        return new TaskFilter
        {
            Priorities = parsedPriorities,
            AssigneeIds = assigneeIds,
            IncludeUnassigned = includeUnassigned,
            DueDate = mode,
            DueFrom = from,
            DueTo = to,
            Types = parsedTypes
        };
    }

    /// <summary>
    /// Paramètres pour <c>NavigationManager.GetUriWithQueryParameters</c>.
    /// Toutes les clés sont présentes : <c>null</c> retire le paramètre de l'URL.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToQueryParameters(TaskFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        string[]? priorities = filter.FiltersPriority
            ? [.. filter.Priorities.Order().Select(p => p.ToString())]
            : null;

        string[]? assignees = null;
        if (filter.FiltersAssignee)
        {
            var ids = filter.AssigneeIds.Order(StringComparer.Ordinal);
            assignees = filter.IncludeUnassigned ? [.. ids, UnassignedValue] : [.. ids];
        }

        string[]? types = filter.FiltersType
            ? [.. filter.Types.Order().Select(t => t.ToString())]
            : null;

        var hasDue = filter.HasDueDateFilter;
        var isRange = hasDue && filter.DueDate == DueDateFilter.Range;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [PriorityParam] = priorities,
            [AssigneeParam] = assignees,
            [TypeParam] = types,
            [DueParam] = hasDue ? SlugByDue[filter.DueDate] : null,
            [DueFromParam] = isRange ? FormatDate(filter.DueFrom) : null,
            [DueToParam] = isRange ? FormatDate(filter.DueTo) : null,
        };
    }

    private static (DueDateFilter Mode, DateOnly? From, DateOnly? To) ParseDueDate(
        string? due, string? dueFrom, string? dueTo)
    {
        if (string.IsNullOrWhiteSpace(due) || !DueBySlug.TryGetValue(due.Trim(), out var mode))
        {
            return (DueDateFilter.Any, null, null);
        }

        if (mode != DueDateFilter.Range)
        {
            return (mode, null, null); // from/to n'ont de sens qu'avec une plage
        }

        var from = ParseDate(dueFrom);
        var to = ParseDate(dueTo);

        if (from is null && to is null)
        {
            return (DueDateFilter.Any, null, null);
        }

        // Bornes inversées : on corrige plutôt que d'ignorer, c'est ce que l'utilisateur voulait.
        return from > to
            ? (DueDateFilter.Range, to, from)
            : (DueDateFilter.Range, from, to);
    }

    private static IEnumerable<string> Bounded(IEnumerable<string>? values) =>
        (values ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Take(MaxValuesPerParam);

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static string? FormatDate(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.InvariantCulture);
}
