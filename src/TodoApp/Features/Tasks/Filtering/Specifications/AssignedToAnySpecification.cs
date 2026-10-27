using System.Linq.Expressions;
using TodoApp.Domain.Tasks;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.Filtering.Specifications;

/// <summary>
/// Tâches assignées à l'un des utilisateurs donnés et/ou non assignées.
/// </summary>
internal sealed class AssignedToAnySpecification : Specification<TaskItem>
{
    private readonly string[] _assigneeIds;
    private readonly bool _includeUnassigned;

    public AssignedToAnySpecification(IReadOnlyCollection<string> assigneeIds, bool includeUnassigned)
    {
        ArgumentNullException.ThrowIfNull(assigneeIds); if (assigneeIds.Count == 0 && !includeUnassigned)
        {
            throw new ArgumentException(
                "Au moins un utilisateur ou l'option « non assignée » est requis.",
                nameof(assigneeIds));
        }

        _assigneeIds = [.. assigneeIds.Distinct(StringComparer.Ordinal)];
        _includeUnassigned = includeUnassigned;
    }

    public override Expression<Func<TaskItem, bool>> ToExpression()
    {
        var ids = _assigneeIds;

        if (ids.Length == 0)
        {
            return t => t.AssigneeId == null;
        }

        if (!_includeUnassigned)
        {
            return t => t.AssigneeId != null && ids.Contains(t.AssigneeId);
        }

        return t => t.AssigneeId == null || ids.Contains(t.AssigneeId);
    }
}
