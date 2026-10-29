using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Errors;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.UpdateTask;

internal sealed class UpdateTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IValidator<UpdateTaskCommand> validator,
    ICurrentUser currentUser,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result> HandleAsync(UpdateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation("Task.IsInvalid", validation.Errors[0].ErrorMessage);
        }

        var createdById = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // Filtre sur le propriétaire : la tâche d'un autre utilisateur est traitée comme inexistante.
        var task = await db.Tasks
            .AsTracking()
            .VisibleTo(createdById)
            .SingleOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        task.Update(command.Title, command.Description, command.Priority, command.DueDate, command.Type);

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return Result.Success();
    }
}
