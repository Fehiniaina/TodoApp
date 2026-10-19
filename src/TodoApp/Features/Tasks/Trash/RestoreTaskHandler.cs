using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.Trash;

internal sealed record RestoreTaskCommand(Guid Id);

internal sealed class RestoreTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(RestoreTaskCommand command, CancellationToken cancellationToken = default)
    {
        var ownerId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == command.Id && t.OwnerId == ownerId && t.IsDeleted, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotInTrash", "Cette tâche n'est pas dans la corbeille.");
        }

        task.Restore();
        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return Result.Success();
    }
}
