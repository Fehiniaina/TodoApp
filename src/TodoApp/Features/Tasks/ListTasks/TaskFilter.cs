using System.Collections.Frozen;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Filtering.Specifications;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>
/// Critères de filtrage de la liste. ET entre critères, OU dans un même critère.
/// Prévu pour accueillir plus tard le type et les tags.
/// Égalité par valeur (y compris le contenu des ensembles).
/// </summary>
internal sealed record TaskFilter
{
    private readonly FrozenSet<TaskPriority> _priorities = FrozenSet<TaskPriority>.Empty;
    private readonly FrozenSet<string> _assigneeIds = FrozenSet<string>.Empty;
    private readonly FrozenSet<TaskType> _types = FrozenSet<TaskType>.Empty;

    public static TaskFilter None { get; } = new();

    public IReadOnlySet<TaskPriority> Priorities
    {
        get => _priorities;
        init => _priorities = (value ?? throw new ArgumentNullException(nameof(value))).ToFrozenSet();
    }

    public IReadOnlySet<TaskType> Types
    {
        get => _types;
        init => _types = (value ?? throw new ArgumentNullException(nameof(value))).ToFrozenSet();
    }

    public IReadOnlySet<string> AssigneeIds
    {
        get => _assigneeIds;
        init => _assigneeIds = (value ?? throw new ArgumentNullException(nameof(value)))
            .ToFrozenSet(StringComparer.Ordinal);
    }

    public bool IncludeUnassigned { get; init; }

    public DueDateFilter DueDate { get; init; }

    /// <summary>Utilisés seulement avec <see cref="DueDateFilter.Range"/>. Bornes incluses.</summary>
    public DateOnly? DueFrom { get; init; }

    public DateOnly? DueTo { get; init; }

    public bool FiltersPriority => _priorities.Count > 0;

    public bool FiltersAssignee => _assigneeIds.Count > 0 || IncludeUnassigned;

    public bool FiltersType => _types.Count > 0;

    /// <summary>Faux pour <see cref="DueDateFilter.Any"/> et pour une plage invalide.</summary>
    public bool HasDueDateFilter => DueDate switch
    {
        DueDateFilter.Any => false,
        DueDateFilter.Range => (DueFrom is not null || DueTo is not null) && !(DueFrom > DueTo),
        _ => Enum.IsDefined(DueDate),
    };

    /// <summary>Nombre de critères actifs, pour le badge « Filtres (2) ».</summary>
    public int ActiveCount =>
        (FiltersPriority ? 1 : 0)
        + (FiltersAssignee ? 1 : 0)
        + (HasDueDateFilter ? 1 : 0)
        + (FiltersType ? 1 : 0);

    public bool IsEmpty => ActiveCount == 0;

    public IEnumerable<Specification<TaskItem>> ToSpecifications(DateOnly today)
    {
        if (FiltersPriority)
        {
            yield return new PriorityInSpecification(_priorities);
        }

        if (FiltersAssignee)
        {
            yield return new AssignedToAnySpecification(_assigneeIds, IncludeUnassigned);
        }

        if (HasDueDateFilter)
        {
            yield return new DueDateSpecification(DueDate, today, DueFrom, DueTo);
        }

        if (FiltersType)
        {
            yield return new TypeInSpecification(_types);
        }
    }

    public bool Equals(TaskFilter? other) =>
        other is not null
        && IncludeUnassigned == other.IncludeUnassigned
        && DueDate == other.DueDate
        && DueFrom == other.DueFrom
        && DueTo == other.DueTo
        && _priorities.SetEquals(other._priorities)
        && _assigneeIds.SetEquals(other._assigneeIds)
        && _types.SetEquals(other._types);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IncludeUnassigned);
        hash.Add(DueDate);
        hash.Add(DueFrom);
        hash.Add(DueTo);
        hash.Add(UnorderedHash(_priorities));
        hash.Add(UnorderedHash(_assigneeIds));
        hash.Add(UnorderedHash(_types));

        return hash.ToHashCode();
    }

    // XOR : indépendant de l'ordre d'énumération, cohérent avec SetEquals.
    private static int UnorderedHash<T>(FrozenSet<T> set)
    {
        var result = set.Count;
        foreach (var item in set)
        {
            result ^= set.Comparer.GetHashCode(item!);
        }
        return result;
    }
}
