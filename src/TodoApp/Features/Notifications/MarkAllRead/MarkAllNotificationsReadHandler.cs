using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Notifications.Shared;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Notifications.MarkAllRead;

internal sealed class MarkAllNotificationsReadHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IHandler
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        DateTime? nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.Notifications
            .For(userId)
            .Unread()
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.ReadAtUtc, nowUtc),
                cancellationToken);
    }
}
