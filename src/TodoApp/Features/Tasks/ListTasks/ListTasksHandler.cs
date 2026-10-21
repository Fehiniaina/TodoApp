using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.ListTasks;

internal sealed class ListTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private readonly int MaxPageSize = 100;

    public async Task<Result<PagedResult<TaskSummary>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var createdById = await currentUser.GetIdAsync();

        var ownerTasks = db.Tasks
            .AsNoTracking()
            .VisibleTo(createdById);

        int totalCount = await ownerTasks.CountAsync(cancellationToken);

        var items = await ownerTasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.Id)
            .Skip((page -1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskSummary(
                t.Id, t.Title, t.Description, t.Priority, t.CreatedAtUtc,
                t.Status, t.CompletedAt, t.DueDate,
                t.Checklist.Count(i => i.IsDone), t.Checklist.Count, t.RecurringTaskId))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskSummary>(items, totalCount, pageSize);
    }
}
