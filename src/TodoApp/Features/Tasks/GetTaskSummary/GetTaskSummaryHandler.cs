using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.ListTasks;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.GetTaskSummary;

internal sealed class GetTaskSummaryHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    public async Task<TaskSummary?> HandleAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var tasks = db.Tasks
            .VisibleTo(userId)
            .Where(t => t.Id == taskId);

        return await TaskSummaryProjection.ToSummaries(tasks, db.Users)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
