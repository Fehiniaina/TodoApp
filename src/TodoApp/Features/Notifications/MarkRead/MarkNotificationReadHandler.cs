using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Notifications.Shared;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Notifications.MarkRead;

internal sealed class MarkNotificationReadHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IHandler
{
    private static readonly Error NotFound =
        Error.NotFound("Notification.NotFound", "Cette notification n'existe plus.");

    public async Task<Result> HandleAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var notification = await db.Notifications
            .AsTracking()
            .For(userId)
            .SingleOrDefaultAsync(n => n.Id == command.NotificationId, cancellationToken);

        if (notification is null)
        {
            return NotFound;
        }

        notification.MarkAsRead(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
