using System.Linq.Expressions;
using Org.BouncyCastle.Crypto;
using TodoApp.Domain.Tasks;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.Filtering.Specifications;

internal sealed class TagsInSpecification(IReadOnlyCollection<Guid> tagsIds)
    : Specification<TaskItem>
{
    public override Expression<Func<TaskItem, bool>> ToExpression()
    {
        var ids = tagsIds.ToArray();

        return t => t.Tags.Any(tt => ids.Contains(tt.TagId));
    }
}
