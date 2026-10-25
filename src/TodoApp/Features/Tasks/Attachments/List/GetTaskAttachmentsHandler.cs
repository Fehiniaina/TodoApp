using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Shared;
using TodoApp.Features.Tasks.Shared;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.Attachments.List;

internal sealed class GetTaskAttachmentsHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private const string DeletedUser = "Utilisateur supprimé";

    public async Task<Result<AttachmentDto[]>> HandleAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        if (!await db.Tasks.VisibleTo(userId).AnyAsync(t => t.Id == taskId, cancellationToken))
        {
            return AttachmentErrors.TaskNotFound;
        }

        return await db.TaskAttachments
            .TagWith("GetTaskAttachments")
            .Where(a => a.TaskId == taskId)
            .LeftJoin(db.Users, a => a.UploadedById, u => u.Id, (a, u) => new { Attachment = a, Uploader = u })
            .OrderByDescending(x => x.Attachment.UploadedAtUtc)
            .ThenByDescending(x => x.Attachment.Id)
            .Select(x => new AttachmentDto(
                x.Attachment.Id,
                x.Attachment.FileName,
                x.Attachment.ContentType,
                x.Attachment.SizeBytes,
                x.Attachment.UploadedAtUtc,
                x.Attachment.UploadedById,
                x.Uploader == null
                    ? DeletedUser
                    : UserSummary.ToDisplayName(x.Uploader.FirstName, x.Uploader.LastName, x.Uploader.Email)))
            .ToArrayAsync(cancellationToken);
    }
}
