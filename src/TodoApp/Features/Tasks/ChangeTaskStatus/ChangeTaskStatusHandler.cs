using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.ChangeTaskStatus;

internal sealed class ChangeTaskStatusHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IHandler
{
    public async Task<Result<ChangeTaskStatusResult>> HandleAsync(
        ChangeTaskStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.NewStatus))
        {
            return Error.Validation("Task.InvalidStatus", "Ce statut n'existe pas.");
        }

        var actorId = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == command.TaskId && t.OwnerId == actorId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        if (task.Status != command.NewStatus && !TaskWorkflow.CanMove(task.Status, command.NewStatus))
        {
            return Error.Validation(
                "Task.InvalidTransition",
                $"Impossible de passer de « {task.Status.ToLabel()} » à « {command.NewStatus.ToLabel()} ».");
        }

        task.ChangeStatus(command.NewStatus, actorId, timeProvider.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(cancellationToken);

        return new ChangeTaskStatusResult(task.Id, task.Status, task.CompletedAt, task.AllowedNextStatuses);
    }
}
