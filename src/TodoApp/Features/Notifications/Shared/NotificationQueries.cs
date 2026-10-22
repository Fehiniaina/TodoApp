using TodoApp.Domain.Notifications;

namespace TodoApp.Features.Notifications.Shared;

internal static class NotificationQueries
{
    public static IQueryable<Notification> For(this IQueryable<Notification> source, string userId) =>
        source.Where(n => n.RecipientId == userId);

    public static IQueryable<Notification> Unread(this IQueryable<Notification> source) =>
        source.Where(n => n.ReadAtUtc == null);
}
