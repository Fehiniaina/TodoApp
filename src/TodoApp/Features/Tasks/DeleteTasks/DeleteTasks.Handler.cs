using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.DeleteTasks;

internal sealed record DeleteTasksCommand(Guid Id, string OwnerId);

internal sealed class DeleteTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    TaskChangeNotifier notifier,
    TimeProvider timeProvider) : IHandler
{
    public async Task<Result> HandleAsync(DeleteTasksCommand command, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var task = await context.Tasks
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == command.Id && t.OwnerId == command.OwnerId, cancellationToken);

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
