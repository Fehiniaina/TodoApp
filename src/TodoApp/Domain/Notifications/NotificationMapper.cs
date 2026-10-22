using TodoApp.Domain.Common;
using TodoApp.Domain.Tasks.Events;

namespace TodoApp.Domain.Notifications;

internal static class NotificationMapper
{
    public static Notification? ToNotification(IDomainEvent domainEvent) => domainEvent switch
    {
        TaskAssigned { NewAssigneeId: { } recipientId } e
            when !string.Equals(recipientId, e.ActorId, StringComparison.Ordinal)            // pas d'auto-assignation
              && !string.Equals(recipientId, e.PreviousAssigneeId, StringComparison.Ordinal) // pas de doublon
            => Notification.ForTaskAssignment(recipientId, e.TaskId, e.ActorId, e.OccurredAtUtc),

        _ => null,
    };
}
