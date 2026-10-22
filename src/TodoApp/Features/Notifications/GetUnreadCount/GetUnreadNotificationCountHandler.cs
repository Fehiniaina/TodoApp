using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Notifications.Shared;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Notifications.GetUnreadCount;

internal sealed class GetUnreadNotificationCountHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    public async Task<int> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetIdAsync();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.Notifications
            .For(userId)
            .Unread()
            .CountAsync(cancellationToken);
    }
}
