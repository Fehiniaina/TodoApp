using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;
using TodoApp.Shared.Specifications;

namespace TodoApp.Features.Tasks.ListTasks;

internal sealed class ListTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<TaskSummary>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var userId = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var tasks = ApplyScope(db.Tasks.VisibleTo(userId), query.Scope, userId)
            .Where(query.Filter.ToSpecifications(query.Today));

        var totalCount = await tasks.CountAsync(cancellationToken);
        var items = await TaskSummaryProjection.ToSummaries(Paginate(tasks, page, pageSize), db.Users)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskSummary>(items, totalCount, pageSize);
    }

    private static IQueryable<TaskItem> ApplyScope(IQueryable<TaskItem> tasks, TaskScope scope, string userId) =>
        scope switch
        {
            TaskScope.AssignedToMe => tasks.Where(t => t.AssigneeId == userId),
            TaskScope.CreatedByMe => tasks.Where(t => t.CreatedById == userId),
            _ => tasks,
        };

    /// <summary>Tri stable (Id en second critère) : aucune tâche ne saute ni ne se répète d'une page à l'autre.</summary>
    private static IQueryable<TaskItem> Paginate(IQueryable<TaskItem> tasks, int page, int pageSize) =>
        tasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
}
