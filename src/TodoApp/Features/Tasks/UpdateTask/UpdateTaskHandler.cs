using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Errors;

namespace TodoApp.Features.Tasks.UpdateTask;

internal sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority);

internal sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty()
                .WithErrorCode(TaskErrors.TitleRequired.Code)
                .WithMessage(TaskErrors.TitleRequired.Message)
            .MaximumLength(200)
                .WithErrorCode(TaskErrors.TitleTooLong.Code)
                .WithMessage(TaskErrors.TitleTooLong.Message);

        RuleFor(c => c.Description)
            .MaximumLength(2000)
                .WithErrorCode(TaskErrors.DescriptionTooLong.Code)
                .WithMessage(TaskErrors.DescriptionTooLong.Message)
            .When(c => c.Description is not null);

        RuleFor(c => c.Priority)
            .IsInEnum()
                .WithErrorCode(TaskErrors.InvalidPriority.Code)
                .WithMessage(TaskErrors.InvalidPriority.Message);
    }
}

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

        var ownerId = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // Filtre sur le propriétaire : la tâche d'un autre utilisateur est traitée comme inexistante.
        var task = await db.Tasks
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == command.Id && t.OwnerId == ownerId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        task.Update(command.Title, command.Description, command.Priority);

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return Result.Success();
    }
}
