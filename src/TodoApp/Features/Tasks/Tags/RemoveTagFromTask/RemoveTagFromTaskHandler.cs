using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Tags.RemoveTagFromTask;

internal sealed class RemoveTagFromTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(RemoveTagFromTaskCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actorId = await currentUser.GetIdAsync();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .ForUpdate()
            .VisibleTo(actorId)
            .SingleOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        // Tag déjà supprimé : la cascade l'a retiré des tâches, il n'y a plus rien à faire.
        var tag = await db.Tags.SingleOrDefaultAsync(t => t.Id == command.TagId, cancellationToken);
        if (tag is null || !task.RemoveTag(tag, actorId, nowUtc))
        {
            return Result.Success(); // idempotent
        }

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();
        return Result.Success();
    }
}
