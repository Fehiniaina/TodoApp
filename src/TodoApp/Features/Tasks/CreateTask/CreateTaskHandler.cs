using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.CreateTask;

internal sealed class CreateTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IValidator<CreateTaskCommand> validator,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result<CreateTaskResult>> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var first = validation.Errors[0];
            return Error.Validation("Task.IsInvalid", first.ErrorMessage);
        }

        var createdById = await currentUser.GetIdAsync();

        var task = TaskItem.Create(
            createdById,
            command.Title.Trim(),
            string.IsNullOrWhiteSpace(command.Description) ? null : command.Description,
            command.Priority,
            command.DueDate,
            timeProvider.GetUtcNow().UtcDateTime,
            command.Type);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        notifier.NotifyChanged();

        return new CreateTaskResult(task.Id, task.Title, task.Description, task.Priority, task.CreatedAtUtc);
    }
}
