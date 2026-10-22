using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Trash;

internal sealed class ListTrashedTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    public async Task<Result<IReadOnlyList<TrashedTaskSummary>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var createdById = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var items = await db.Tasks
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .CreatedBy(createdById)
            .Where(t => t.IsDeleted)
            .OrderByDescending(t => t.DeletedAt)
            .Select(t => new TrashedTaskSummary(t.Id, t.Title, t.Priority, t.DeletedAt!.Value))
            .ToListAsync(cancellationToken);

        return items;
    }
}
