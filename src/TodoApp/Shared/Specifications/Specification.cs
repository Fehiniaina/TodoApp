using System.Linq.Expressions;

namespace TodoApp.Shared.Specifications;

public abstract class Specification<T>
{
    private Func<T, bool>? _compiled;

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T candidate) =>
        (_compiled ??= ToExpression().Compile())(candidate);
}
