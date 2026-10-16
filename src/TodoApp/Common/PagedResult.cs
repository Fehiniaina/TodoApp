namespace TodoApp.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
