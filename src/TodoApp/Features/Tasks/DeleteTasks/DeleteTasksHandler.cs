using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.DeleteTasks;

internal sealed class DeleteTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    TaskChangeNotifier notifier,
    TimeProvider timeProvider,
    ICurrentUser currentUser) : IHandler
{
    public async Task<Result> HandleAsync(DeleteTasksCommand command, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var createdById = await currentUser.GetIdAsync();

        var task = await context.Tasks
            .AsTracking()
            .CreatedBy(createdById)
            .SingleOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task,NotFound", "Cette tâche n'existe pas ou a déjà été supprimée.");
        }

        task.MoveToTrash(timeProvider.GetUtcNow().UtcDateTime);

        await context.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return Result.Success();
    }
}
