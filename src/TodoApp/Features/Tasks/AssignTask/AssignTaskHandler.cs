using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.AssignTask;

internal sealed class AssignTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(AssignTaskCommand command, CancellationToken cancellationToken = default)
    {
        var actorId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .VisibleTo(actorId)
            .SingleOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        if (command.AssigneeId is not null
            && !await db.Users.AnyAsync(u => u.Id == command.AssigneeId, cancellationToken))
        {
            return Error.Validation("Task.UnknownAssignee", "Cet utilisateur n'existe pas.");
        }

        task.AssignTo(command.AssigneeId, actorId, timeProvider.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();   // la tâche apparaît chez le nouvel assigné, disparaît chez l'ancien

        return Result.Success();
    }
}
