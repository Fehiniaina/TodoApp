using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.Features.Tasks.Attachments.Delete;

internal sealed partial class DeleteTaskAttachmentHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<DeleteTaskAttachmentHandler> logger) : IHandler
{
    public async Task<Result> HandleAsync(DeleteTaskAttachmentCommand command, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var task = await db.Tasks
            .AsTracking()
            .VisibleTo(userId)
            .Include(t => t.Attachments)
            .SingleOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return AttachmentErrors.TaskNotFound;
        }

        var attachment = task.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId);
        if (attachment is null)
        {
            return AttachmentErrors.NotFound;
        }

        var canDelete = string.Equals(attachment.UploadedById, userId, StringComparison.Ordinal)
                     || string.Equals(task.CreatedById, userId, StringComparison.Ordinal);
        if (!canDelete)
        {
            return AttachmentErrors.CannotDelete;
        }

        var removed = task.RemoveAttachment(attachment.Id, userId, timeProvider.GetUtcNow().UtcDateTime);

        // La base d'abord : si la suppression du fichier échoue ensuite, on a un orphelin (nettoyé en B7),
        // jamais une ligne qui pointe vers un fichier absent.
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await storage.DeleteAsync(removed.StorageKey, CancellationToken.None);
        }
        catch (IOException ex)
        {
            LogFileNotDeleted(logger, ex, removed.StorageKey);
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fichier {StorageKey} non supprimé : il sera retiré par le nettoyage des orphelins")]
    private static partial void LogFileNotDeleted(ILogger logger, Exception exception, string storageKey);
}
