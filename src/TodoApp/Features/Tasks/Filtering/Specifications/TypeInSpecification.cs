using System.Linq.Expressions;
using TodoApp.Domain.Tasks;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.Filtering.Specifications;

internal sealed class TypeInSpecification(IReadOnlyCollection<TaskType> types)
    : Specification<TaskItem>
{
    public override Expression<Func<TaskItem, bool>> ToExpression()
    {
        var values = types.ToArray();

        return t => values.Contains(t.Type);
    }
}
