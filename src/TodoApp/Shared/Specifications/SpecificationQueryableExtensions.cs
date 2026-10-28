namespace TodoApp.Shared.Specifications;

public static class SpecificationQueryableExtensions
{
    public static IQueryable<T> Where<T>(this IQueryable<T> query, IEnumerable<Specification<T>> specs)
    {
        ArgumentNullException.ThrowIfNull(specs);

        foreach (var spec in specs)
        {
            query = query.Where(spec.ToExpression());
        }

        return query;
    }
}
