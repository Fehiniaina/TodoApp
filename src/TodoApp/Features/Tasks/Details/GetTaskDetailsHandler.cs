using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.Details;

internal sealed class GetTaskDetailsHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private static readonly Error NotFound =
        Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");

    public async Task<Result<TaskDetails>> HandleAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var details = await db.Tasks
            .TagWith("GetTaskDetails")
            .VisibleTo(userId)
            .Where(t => t.Id == taskId)
            .LeftJoin(db.Users, t => t.AssigneeId, u => u.Id, (t, a) => new { Task = t, Assignee = a })
            .Join(db.Users, x => x.Task.CreatedById, u => u.Id, (x, c) => new { x.Task, x.Assignee, Creator = c })
            .Select(x => new TaskDetails(
                x.Task.Id,
                x.Task.Title,
                x.Task.Description,
                x.Task.Priority,
                x.Task.Status,
                x.Task.DueDate,
                x.Task.CreatedAtUtc,
                x.Task.CompletedAt,
                x.Task.RecurringTaskId,
                x.Task.CreatedById,
                UserSummary.ToDisplayName(x.Creator.FirstName, x.Creator.LastName, x.Creator.Email),
                x.Task.AssigneeId,
                x.Assignee == null
                    ? null
                    : UserSummary.ToDisplayName(x.Assignee.FirstName, x.Assignee.LastName, x.Assignee.Email),
                x.Task.Checklist.Count(i => i.IsDone),
                x.Task.Checklist.Count,
                x.Task.Type))
            .SingleOrDefaultAsync(cancellationToken);

        if (details is null)
        {
            return NotFound;
        }

        return details;
    }
}
