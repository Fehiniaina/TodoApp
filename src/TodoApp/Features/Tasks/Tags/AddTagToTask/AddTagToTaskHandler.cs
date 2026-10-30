using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;
using TodoApp.Features.Tags.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Tags.AddTagToTask;

internal sealed class AddTagToTaskHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    IValidator<AddTagToTaskCommand> validator,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier) : IHandler
{
    public async Task<Result<TagDto>> HandleAsync(AddTagToTaskCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.Errors[0].ErrorCode, validation.Errors[0].ErrorMessage);
        }

        var actorId = await currentUser.GetIdAsync();

        try
        {
            return await AddAsync(command, actorId, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Deux utilisateurs ont créé le même tag au même instant : l'index unique a refusé le second.
            // Le tag existe maintenant : on rejoue une fois, il sera simplement réutilisé.
            return await AddAsync(command, actorId, cancellationToken);
        }
    }

    private async Task<Result<TagDto>> AddAsync(AddTagToTaskCommand command, string actorId, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .VisibleTo(actorId)
            .SingleOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");
        }

        var normalized = Tag.Normalize(command.TagName);
        var tag = await db.Tags.SingleOrDefaultAsync(t => t.NormalizedName == normalized, cancellationToken);

        if (tag is null)
        {
            tag = Tag.Create(command.TagName, TagPalette.For(normalized), actorId, nowUtc);
            db.Tags.Add(tag);
        }

        var alreadyTagged = task.Tags.Any(t => t.TagId == tag.Id);
        if (!alreadyTagged && task.Tags.Count >= TaskItem.MaxTags)
        {
            return Error.Validation("Task.TooManyTags", $"Une tâche ne peut pas avoir plus de {TaskItem.MaxTags} tags.");
        }

        task.AddTag(tag, actorId, nowUtc);
        await db.SaveChangesAsync(cancellationToken);

        notifier.NotifyChanged();
        return new TagDto(tag.Id, tag.Name, tag.Color);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
