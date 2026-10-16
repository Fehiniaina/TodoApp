using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.DeleteTasks;

internal sealed record DeleteTasksCommand(Guid Id, string OwnerId);

internal sealed class DeleteTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(DeleteTasksCommand command, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var taskItem = await context.Tasks
            .Where(t => t.Id == command.Id && t.OwnerId == command.OwnerId)
            .ExecuteDeleteAsync(cancellationToken);

        if (taskItem == 0)
        {
            return Error.NotFound("Task,NotFound", "Cette tâche n'existe pas");
        }

        notifier.NotifyChanged();

        return Result.Success();
    }
}
