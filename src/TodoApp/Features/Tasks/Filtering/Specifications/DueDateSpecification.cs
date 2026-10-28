using System.Linq.Expressions;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.Filtering.Specifications;

/// <summary>
/// Filtre sur la date d'échéance, relatif à <c>today</c> (fourni par l'appelant via TimeProvider).
/// Bornes inclusives pour <see cref="DueDateFilter.Range"/>.
/// </summary>
internal sealed class DueDateSpecification : Specification<TaskItem>
{
    /// <summary>Taille de la fenêtre « 7 prochains jours » : aujourd'hui inclus, J+6 inclus.</summary>
    public const int UpcomingWindowDays = 7;

    private readonly DueDateFilter _mode;
    private readonly DateOnly? _from;
    private readonly DateOnly? _to;
    private readonly DateOnly _today;

    public DueDateSpecification(DueDateFilter mode, DateOnly today, DateOnly? from = null, DateOnly? to = null)
    {
        if (mode == DueDateFilter.Any || !Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Mode de filtre d'échéance invalide.");
        }

        if (mode == DueDateFilter.Range)
        {
            if (from is null && to is null)
            {
                throw new ArgumentException("Une plage requiert au moins une borne.", nameof(from));
            }

            if (from > to)
            {
                throw new ArgumentException("La date de début doit précéder la date de fin.", nameof(from));
            }
        }

        _mode = mode;
        _today = today;
        _from = from;
        _to = to;
    }

    public override Expression<Func<TaskItem, bool>> ToExpression()
    {
        var today = _today;
        var windowEnd = today.AddDays(UpcomingWindowDays - 1);
        var from = _from;
        var to = _to;

        return _mode switch
        {
            DueDateFilter.Overdue => t => t.DueDate != null && t.DueDate < today && t.Status != TaskItemStatus.Done,
            DueDateFilter.Today => t => t.DueDate == today,
            DueDateFilter.Next7Days => t => t.DueDate >= today && t.DueDate <= windowEnd,
            DueDateFilter.NoDueDate => t => t.DueDate == null,
            DueDateFilter.Range => RangeExpression(from, to),
            _ => throw new InvalidOperationException($"Mode non géré : {_mode}."),
        };
    }

    private static Expression<Func<TaskItem, bool>> RangeExpression(DateOnly? from, DateOnly? to) =>
        (from, to) switch
        {
            ({ } f, { } e) => t => t.DueDate >= f && t.DueDate <= e,
            ({ } f, null) => t => t.DueDate >= f,
            (null, { } e) => t => t.DueDate <= e,
            _ => throw new InvalidOperationException("Plage sans borne."),
        };
}
