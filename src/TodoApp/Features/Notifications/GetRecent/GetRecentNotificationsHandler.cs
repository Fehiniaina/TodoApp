using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Notifications.Shared;
using TodoApp.Features.Shared;
using TodoApp.Features.Users;

namespace TodoApp.Features.Notifications.GetRecent;

internal sealed class GetRecentNotificationsHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser) : IHandler
{
    private const int MaxTake = 50;
    private const string SystemActor = "Système";

    public async Task<IReadOnlyList<NotificationDto>> HandleAsync(
        GetRecentNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, MaxTake);
        var userId = await currentUser.GetIdAsync();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.Notifications
            .For(userId)
            .LeftJoin(
                db.Users,
                n => n.ActorId,
                u => u.Id,
                (n, actor) => new { Notification = n, Actor = actor })
            .OrderByDescending(x => x.Notification.CreatedAtUtc)
            .ThenByDescending(x => x.Notification.Id)
            .Take(take)
            .Select(x => new NotificationDto(
                x.Notification.Id,
                x.Notification.Type,
                x.Notification.TaskId,
                x.Notification.Task.Title,
                x.Notification.ActorId,
                x.Actor == null
                    ? SystemActor
                    : UserSummary.ToDisplayName(x.Actor.FirstName, x.Actor.LastName, x.Actor.Email),
                x.Notification.CreatedAtUtc,
                x.Notification.ReadAtUtc != null))
            .ToListAsync(cancellationToken);
    }
}
