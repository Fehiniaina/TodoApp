using System.Linq.Expressions;
using TodoApp.Domain.Tasks;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.Filtering.Specifications;

internal sealed class PriorityInSpecification(IReadOnlyCollection<TaskPriority> priorities)
    : Specification<TaskItem>
{
    public override Expression<Func<TaskItem, bool>> ToExpression()
    {
        var values = priorities.ToArray();

        return t => values.Contains(t.Priority);
    }
}
