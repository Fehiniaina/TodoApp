using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.Recurrence;

internal sealed class StopRecurringTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    public async Task<Result> HandleAsync(Guid recurringTaskId, CancellationToken cancellationToken = default)
    {
        var ownerId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var series = await db.RecurringTasks
            .AsTracking()
            .SingleOrDefaultAsync(s => s.Id == recurringTaskId && s.OwnerId == ownerId, cancellationToken);

        if (series is null)
        {
            return Error.NotFound("RecurringTask.NotFound", "Cette récurrence n'existe pas.");
        }

        series.Stop();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
