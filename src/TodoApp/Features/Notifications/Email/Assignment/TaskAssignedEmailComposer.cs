using TodoApp.Domain.Notifications;
using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Features.Notifications.Email.Assignment;

internal sealed class TaskAssignedEmailComposer : INotificationEmailComposer
{
    public NotificationType Type => NotificationType.TaskAssigned;

    public IEmailModel Compose(NotificationEmailContext context) => new TaskAssignedEmail(
        context.RecipientName,
        context.ActorName,
        context.TaskTitle,
        context.TaskUrl);
}
