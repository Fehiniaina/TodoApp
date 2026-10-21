using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Trash;

internal sealed class RestoreTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(RestoreTaskCommand command, CancellationToken cancellationToken = default)
    {
        var createdById = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .AsTracking()
            .CreatedBy(createdById)
            .SingleOrDefaultAsync(t => t.Id == command.Id && t.IsDeleted, cancellationToken);

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
