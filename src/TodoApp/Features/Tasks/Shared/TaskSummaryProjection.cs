using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.Shared;

internal static class TaskSummaryProjection
{
    /// <summary>
    /// Projection finale : tout est traduit en SQL, sauf ToDisplayName qu'EF évalue
    /// en mémoire sur chaque ligne (autorisé dans le dernier Select uniquement).
    /// </summary>
    public static IQueryable<TaskSummary> ToSummaries(IQueryable<TaskItem> tasks, IQueryable<ApplicationUser> users) =>
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
