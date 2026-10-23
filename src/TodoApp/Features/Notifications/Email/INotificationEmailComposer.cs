using TodoApp.Domain.Notifications;
using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Features.Notifications.Email;

internal interface INotificationEmailComposer
{
    NotificationType Type { get; }

    IEmailModel Compose(NotificationEmailContext context);
}
