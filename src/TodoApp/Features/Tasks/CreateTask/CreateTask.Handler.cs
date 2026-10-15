using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.CreateTask;

internal sealed record CreateTaskCommand(
    string OwnerId,
    string Title,
    string? Description,
    TaskPriority Priority);

internal sealed record CreateTaskResult(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime CreatedAtUtc);

internal sealed class CreateTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IValidator<CreateTaskCommand> validator,
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
            return Error.Validation(first.ErrorCode, first.ErrorMessage);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = TaskItem.Create(
            command.OwnerId,
            command.Title.Trim(),
            string.IsNullOrWhiteSpace(command.Description) ? null : command.Description,
            command.Priority);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        notifier.NotifyChanged();

        return new CreateTaskResult(task.Id, task.Title, task.Description, task.Priority, task.CreatedAtUtc);
    }
}
