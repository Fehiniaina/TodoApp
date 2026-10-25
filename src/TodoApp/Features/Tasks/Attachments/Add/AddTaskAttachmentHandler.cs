using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.Features.Tasks.Attachments.Add;

internal sealed partial class AddTaskAttachmentHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<AddTaskAttachmentHandler> logger) : IHandler
{
    private const int CopyBufferSize = 81_920;

    private static readonly Error TooLarge =
        Error.Validation("Attachment.TooLarge", $"Le fichier dépasse {TaskAttachment.MaxSizeBytes / (1024 * 1024)} Mo.");

    public async Task<Result<Guid>> HandleAsync(AddTaskAttachmentCommand command, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // 1. Accès et limite, AVANT de lire le moindre octet.
        var task = await db.Tasks
            .AsTracking()
            .VisibleTo(userId)
            .Include(t => t.Attachments)
            .SingleOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return AttachmentErrors.TaskNotFound;
        }

        if (task.Attachments.Count >= TaskItem.MaxAttachments)
        {
            return AttachmentErrors.LimitReached;
        }

        // 2. Lecture bornée : la taille réelle compte, pas celle annoncée par le navigateur.
        await using var buffer = await ReadBoundedAsync(command.Content, TaskAttachment.MaxSizeBytes, cancellationToken);
        if (buffer is null)
        {
            return TooLarge;
        }

        // 3. Validation : nom, extension, signature des premiers octets.
        var validation = Validate(command.FileName, buffer);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        // 4. Fichier d'abord, base ensuite.
        var storageKey = await storage.SaveAsync(buffer, cancellationToken);

        try
        {
            var attachment = task.AddAttachment(
                validation.Value.FileName,
                validation.Value.ContentType,
                buffer.Length,
                storageKey,
                userId,
                timeProvider.GetUtcNow().UtcDateTime);

            await db.SaveChangesAsync(cancellationToken);

            LogAdded(logger, attachment.Id, task.Id, attachment.SizeBytes);
            return attachment.Id;
        }
        catch
        {
            // Compensation : la ligne n'existe pas, le fichier ne doit pas rester.
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    /// <summary>Copie au plus <paramref name="maxBytes"/> octets ; <c>null</c> si le flux est plus long.</summary>
    private static async Task<MemoryStream?> ReadBoundedAsync(Stream source, long maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[CopyBufferSize];

        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                await buffer.DisposeAsync();
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        buffer.Position = 0;
        return buffer;
    }

    // Méthode synchrone : un ReadOnlySpan ne peut pas traverser un await.
    private static Result<ValidatedAttachment> Validate(string fileName, MemoryStream buffer)
    {
        var headerLength = (int)Math.Min(buffer.Length, AttachmentFileValidator.HeaderLength);
        var header = buffer.GetBuffer().AsSpan(0, headerLength);

        return AttachmentFileValidator.Validate(fileName, buffer.Length, header);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pièce jointe {AttachmentId} ajoutée à la tâche {TaskId} ({SizeBytes} octets)")]
    private static partial void LogAdded(ILogger logger, Guid attachmentId, Guid taskId, long sizeBytes);
}
