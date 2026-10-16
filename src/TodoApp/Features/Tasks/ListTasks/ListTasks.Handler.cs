using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

internal sealed record ListTasksQuery(
    string OwnerId,
    string? Priority = default,
    int Page = 1,
    int PageSize = 10);

internal sealed record TaskSummary(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime CreatedAtUtc);

internal sealed class ListTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory) : IHandler
{
    private int MaxPageSize = 100;

    public async Task<Result<PagedResult<TaskSummary>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var ownerTasks = db.Tasks
            .AsNoTracking()
            .Where(t => t.OwnerId == query.OwnerId);

        int totalCount = await ownerTasks.CountAsync(cancellationToken);

        var items = await ownerTasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenBy(t => t.Id)
            .Skip((page -1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskSummary(t.Id, t.Title, t.Description, t.Priority, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskSummary>(items, totalCount, pageSize);
    }
}
