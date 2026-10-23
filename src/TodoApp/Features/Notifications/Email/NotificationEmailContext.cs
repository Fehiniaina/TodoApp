using TodoApp.Domain.Notifications;

namespace TodoApp.Features.Notifications.Email;

internal sealed record NotificationEmailContext(
    Notification Notification,
    string RecipientName,
    string ActorName,
    Uri AppBaseUrl)
{
    public Uri TaskUrl => new(AppBaseUrl, $"tasks?task={Notification.TaskId}");
}
