using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.Features.Tasks.Attachments.Download;

internal sealed partial class GetAttachmentContentHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    IFileStorage storage,
    ILogger<GetAttachmentContentHandler> logger) : IHandler
{
    public async Task<Result<AttachmentContent>> HandleAsync(
        Guid taskId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // VisibleTo est appliqué hors de l'expression : EF intègre alors la sous-requête au SQL.
        var visibleTasks = db.Tasks.VisibleTo(userId);

        var attachment = await db.TaskAttachments
            .TagWith("GetAttachmentContent")
            .Where(a => a.Id == attachmentId && a.TaskId == taskId)
            .Where(a => visibleTasks.Any(t => t.Id == a.TaskId))
            .Select(a => new { a.FileName, a.ContentType, a.StorageKey })
            .SingleOrDefaultAsync(cancellationToken);

        if (attachment is null)
        {
            return AttachmentErrors.NotFound;
        }

        var stream = await storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (stream is null)
        {
            LogFileMissing(logger, attachmentId, attachment.StorageKey);
            return AttachmentErrors.NotFound;
        }

        return new AttachmentContent(stream, attachment.FileName, attachment.ContentType);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fichier absent du stockage pour la pièce jointe {AttachmentId} ({StorageKey})")]
    private static partial void LogFileMissing(ILogger logger, Guid attachmentId, string storageKey);
}
