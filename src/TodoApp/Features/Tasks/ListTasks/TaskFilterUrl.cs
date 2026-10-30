using System.Globalization;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>
/// Aller-retour <see cref="TaskFilter"/> ⇄ query string.
/// Lecture tolérante : toute valeur invalide est ignorée (une URL bricolée ne lève jamais d'exception),
/// mais SIGNALÉE via <c>ignored</c> pour qu'une rupture de compatibilité ne passe pas inaperçue.
/// Écriture canonique : slugs stables, ordre stable, pour que deux filtres égaux produisent la même URL.
/// </summary>
internal static class TaskFilterUrl
{
    public const string PriorityParam = "priority";
    public const string AssigneeParam = "assignee";
    public const string TypeParam = "type";
    public const string DueParam = "due";
    public const string DueFromParam = "from";
    public const string DueToParam = "to";

    /// <summary>Valeur spéciale de <c>assignee</c> pour « non assignée » (les ids Identity sont des GUID).</summary>
    public const string UnassignedValue = "none";

    private const string DateFormat = "yyyy-MM-dd";
    private const int MaxValuesPerParam = 50;
    private const int MaxIdLength = 64;

    // Slugs PERSISTÉS chez les utilisateurs (favoris, liens partagés) : ne JAMAIS modifier un slug existant.
    // Choisis égaux aux anciens noms des membres : les liens d'avant les slugs fonctionnent toujours
    // (lecture insensible à la casse). Aucun slug numérique : « ?type=0 » est refusé.
    // Contrat vérifié par TaskFilterUrlSlugContractTests.
    internal static readonly SlugMap<TaskType> TypeSlugs = new(new Dictionary<TaskType, string>
    {
        [TaskType.Bug] = "bug",
        [TaskType.Extraction] = "extraction",
        [TaskType.Analyse] = "analyse",
        [TaskType.Task] = "task",
        [TaskType.UserStory] = "story", // renommé Story → UserStory : le slug, lui, ne change pas
    });

    internal static readonly SlugMap<TaskPriority> PrioritySlugs = new(new Dictionary<TaskPriority, string>
    {
        [TaskPriority.Low] = "low",
        [TaskPriority.Medium] = "medium",
        [TaskPriority.High] = "high",
        [TaskPriority.Urgent] = "urgent",
    });

    /// <summary><see cref="DueDateFilter.Any"/> n'a pas de slug : c'est l'absence du paramètre.</summary>
    internal static readonly SlugMap<DueDateFilter> DueSlugs = new(
        new Dictionary<DueDateFilter, string>
        {
            [DueDateFilter.Overdue] = "overdue",
            [DueDateFilter.Today] = "today",
            [DueDateFilter.Next7Days] = "next7days",
            [DueDateFilter.NoDueDate] = "none",
            [DueDateFilter.Range] = "range",
        },
        DueDateFilter.Any);

    /// <summary>Lecture sans signalement : délègue à la surcharge complète (une seule implémentation).</summary>
    public static TaskFilter Parse(
        IEnumerable<string>? priorities,
        IEnumerable<string>? assignees,
        IEnumerable<string>? types,
        string? due,
        string? dueFrom,
        string? dueTo) =>
        Parse(priorities, assignees, types, due, dueFrom, dueTo, out _);

    /// <param name="ignored">Valeurs présentes dans l'URL mais non reconnues (lien ancien ou bricolé).</param>
    public static TaskFilter Parse(
        IEnumerable<string>? priorities,
        IEnumerable<string>? assignees,
        IEnumerable<string>? types,
        string? due,
        string? dueFrom,
        string? dueTo,
        out IReadOnlyList<string> ignored)
    {
        var rejected = new List<string>();

        var parsedPriorities = ParseSlugs(priorities, PrioritySlugs, rejected);
        var (assigneeIds, includeUnassigned) = ParseAssignees(assignees, rejected);
        var parsedTypes = ParseSlugs(types, TypeSlugs, rejected);
        var (mode, from, to) = ParseDueDate(due, dueFrom, dueTo, rejected);

        ignored = rejected;

        return new TaskFilter
        {
            Priorities = parsedPriorities,
            AssigneeIds = assigneeIds,
            IncludeUnassigned = includeUnassigned,
            Types = parsedTypes,
            DueDate = mode,
            DueFrom = from,
            DueTo = to,
        };
    }

    /// <summary>
    /// Paramètres pour <c>NavigationManager.GetUriWithQueryParameters</c>.
    /// Toutes les clés sont présentes : <c>null</c> retire le paramètre de l'URL.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToQueryParameters(TaskFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var hasDue = filter.HasDueDateFilter;
        var isRange = hasDue && filter.DueDate == DueDateFilter.Range;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [PriorityParam] = ToSlugs(filter.Priorities, PrioritySlugs),
            [AssigneeParam] = ToAssigneeValues(filter),
            [TypeParam] = ToSlugs(filter.Types, TypeSlugs),
            [DueParam] = hasDue ? DueSlugs.ToSlug(filter.DueDate) : null,
            [DueFromParam] = isRange ? FormatDate(filter.DueFrom) : null,
            [DueToParam] = isRange ? FormatDate(filter.DueTo) : null,
        };
    }

    // --- Lecture ---------------------------------------------------------------

    /// <summary>Lecture d'un paramètre multi-valué par slugs ; les valeurs inconnues vont dans <paramref name="rejected"/>.</summary>
    private static HashSet<TEnum> ParseSlugs<TEnum>(
        IEnumerable<string>? values,
        SlugMap<TEnum> slugs,
        List<string> rejected)
        where TEnum : struct, Enum
    {
        var parsed = new HashSet<TEnum>();

        foreach (var value in Bounded(values))
        {
            if (slugs.TryParse(value, out var item))
            {
                parsed.Add(item);
            }
            else
            {
                rejected.Add(value);
            }
        }

        return parsed;
    }

    private static (HashSet<string> Ids, bool IncludeUnassigned) ParseAssignees(
        IEnumerable<string>? values,
        List<string> rejected)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var includeUnassigned = false;

        foreach (var value in Bounded(values))
        {
            if (string.Equals(value, UnassignedValue, StringComparison.OrdinalIgnoreCase))
            {
                includeUnassigned = true;
            }
            else if (value.Length <= MaxIdLength)
            {
                ids.Add(value);
            }
            else
            {
                rejected.Add(value);
            }
        }

        return (ids, includeUnassigned);
    }

    private static (DueDateFilter Mode, DateOnly? From, DateOnly? To) ParseDueDate(
        string? due,
        string? dueFrom,
        string? dueTo,
        List<string> rejected)
    {
        if (string.IsNullOrWhiteSpace(due))
        {
            return (DueDateFilter.Any, null, null);
        }

        if (!DueSlugs.TryParse(due.Trim(), out var mode))
        {
            rejected.Add(due.Trim());
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

    // --- Écriture --------------------------------------------------------------

    /// <summary>Slugs triés par valeur d'enum (URL canonique), ou <c>null</c> pour retirer le paramètre.</summary>
    private static string[]? ToSlugs<TEnum>(IReadOnlySet<TEnum> values, SlugMap<TEnum> slugs)
        where TEnum : struct, Enum =>
        values.Count == 0 ? null : [.. values.Order().Select(slugs.ToSlug)];

    private static string[]? ToAssigneeValues(TaskFilter filter)
    {
        if (!filter.FiltersAssignee)
        {
            return null;
        }

        var ids = filter.AssigneeIds.Order(StringComparer.Ordinal);
        return filter.IncludeUnassigned ? [.. ids, UnassignedValue] : [.. ids];
    }

    private static string? FormatDate(DateOnly? date) =>
        date?.ToString(DateFormat, CultureInfo.InvariantCulture);
}
