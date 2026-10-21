using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Checklist;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Checklist;

internal sealed class ChecklistHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TaskChangeNotifier notifier) : IHandler
{
    private static readonly Error TaskNotFound =
        Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");

    private static readonly Error ItemNotFound =
        Error.NotFound("Checklist.ItemNotFound", "Cette étape n'existe plus.");

    public async Task<IReadOnlyList<ChecklistItemDto>> GetAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var createdById = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.Tasks
            .VisibleTo(createdById)
            .Where(t => t.Id == taskId)
            .SelectMany(t => t.Checklist)
            .OrderBy(i => i.Position)
            .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsDone))
            .ToListAsync(cancellationToken);
    }

    public Task<Result<IReadOnlyList<ChecklistItemDto>>> AddAsync(Guid taskId, string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Task.FromResult<Result<IReadOnlyList<ChecklistItemDto>>>(
                Error.Validation("Checklist.TitleRequired", "Le libellé de l'étape est obligatoire."));
        }

        if (title.Trim().Length > 200)
        {
            return Task.FromResult<Result<IReadOnlyList<ChecklistItemDto>>>(
                Error.Validation("Checklist.TitleTooLong", "Le libellé ne doit pas dépasser 200 caractères."));
        }

        return MutateAsync(taskId, task =>
        {
            if (task.Checklist.Count >= TaskItem.MaxChecklistItems)
            {
                return Error.Validation("Checklist.TooManyItems",
                    $"Une tâche ne peut pas contenir plus de {TaskItem.MaxChecklistItems} étapes.");
            }

            task.AddChecklistItem(title);
            return null;
        }, cancellationToken);
    }

    public Task<Result<IReadOnlyList<ChecklistItemDto>>> SetDoneAsync(Guid taskId, Guid itemId, bool isDone, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.SetChecklistItemDone(itemId, isDone) ? null : ItemNotFound, cancellationToken);

    public Task<Result<IReadOnlyList<ChecklistItemDto>>> RemoveAsync(Guid taskId, Guid itemId, CancellationToken cancellationToken = default) =>
        MutateAsync(taskId, task => task.RemoveChecklistItem(itemId) ? null : ItemNotFound, cancellationToken);

    /// <summary>Charge l'agrégat, applique la modification, enregistre, renvoie la checklist à jour.</summary>
    private async Task<Result<IReadOnlyList<ChecklistItemDto>>> MutateAsync(
        Guid taskId, Func<TaskItem, Error?> mutate, CancellationToken cancellationToken)
    {
        var createdBy = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .SingleOrDefaultAsync(t => t.Id == taskId && t.CreatedById == createdBy, cancellationToken);

        if (task is null) return TaskNotFound;

        if (mutate(task) is { } error)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged();

        return task.Checklist
            .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsDone))
            .ToList();
    }
}
