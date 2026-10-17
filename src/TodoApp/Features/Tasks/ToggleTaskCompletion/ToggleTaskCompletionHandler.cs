using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.ToggleTaskCompletion;

internal sealed record ToggleTaskCompletionCommand(Guid Id, bool IsCompleted);

internal sealed record ToggleTaskCompletionResult(Guid Id, bool IsCompleted, DateTime? CompletedAt);

internal sealed class ToggleTaskCompletionHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IHandler
{
    public async Task<Result<ToggleTaskCompletionResult>> HandleAsync(
        ToggleTaskCompletionCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownerId = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == command.Id && t.OwnerId == ownerId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        if (command.IsCompleted)
        {
            task.Complete(timeProvider.GetUtcNow().UtcDateTime);
        }
        else
        {
            task.Reopen();
        }

        await db.SaveChangesAsync(cancellationToken);

        // Pas de notifier.NotifyChanged() : TaskList met à jour la ligne elle-même.
        // Un rechargement complet annulerait l'intérêt de la mise à jour partielle.
        return new ToggleTaskCompletionResult(task.Id, task.IsCompleted, task.CompletedAt);
    }
}
