using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Trash;

internal sealed class PurgeTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    public async Task<Result> HandleAsync(PurgeTaskCommand command, CancellationToken cancellationToken = default)
    {
        var createdById = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var deleted = await db.Tasks
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .CreatedBy(createdById)
            .Where(t => t.Id == command.Id && t.IsDeleted)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0
            ? Error.NotFound("Task.NotInTrash", "Cette tâche n'est pas dans la corbeille.")
            : Result.Success();
    }
}
