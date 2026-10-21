using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;

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

        var tasks = ApplyScope(db.Tasks.VisibleTo(userId), query.Scope, userId);

        var totalCount = await tasks.CountAsync(cancellationToken);
        var items = await ToSummaries(Paginate(tasks, page, pageSize), db.Users)
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

    /// <summary>
    /// Projection finale : tout est traduit en SQL, sauf ToDisplayName qu'EF évalue
    /// en mémoire sur chaque ligne (autorisé dans le dernier Select uniquement).
    /// </summary>
    private static IQueryable<TaskSummary> ToSummaries(IQueryable<TaskItem> tasks, IQueryable<ApplicationUser> users) =>
        tasks
            .LeftJoin(users, t => t.AssigneeId, u => u.Id, (t, u) => new { Task = t, Assignee = u })
            .Select(x => new TaskSummary(
                x.Task.Id,
                x.Task.Title,
                x.Task.Description,
                x.Task.Priority,
                x.Task.CreatedAtUtc,
                x.Task.Status,
                x.Task.CompletedAt,
                x.Task.DueDate,
                x.Task.Checklist.Count(i => i.IsDone),
                x.Task.Checklist.Count,
                x.Task.RecurringTaskId,
                x.Task.CreatedById,
                x.Task.AssigneeId,
                x.Assignee == null
                    ? null
                    : UserSummary.ToDisplayName(x.Assignee.FirstName, x.Assignee.LastName, x.Assignee.Email)));
}
