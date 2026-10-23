using TodoApp.Domain.Tasks;

namespace TodoApp.Domain.Notifications;

internal sealed class Notification
{
    // EF Core
    private Notification()
    {
        RecipientId = null!;
    }

    private Notification(
        string recipientId,
        NotificationType type,
        Guid taskId,
        string? actorId,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        RecipientId = recipientId;
        Type = type;
        TaskId = taskId;
        ActorId = actorId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public string? ActorId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public bool IsRead => ReadAtUtc is not null;

    public EmailDelivery? Email { get; private set; }

    public static Notification ForTaskAssignment(
        string recipientId,
        Guid taskId,
        string? actorId,
        DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);

        if (taskId == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant de la tâche est requis.", nameof(taskId));
        }

        var notification = new Notification(
            recipientId,
            NotificationType.TaskAssigned,
            taskId,
            actorId,
            EnsureUtc(occurredAtUtc));

        notification.Email = EmailDelivery.Schedule(notification.CreatedAtUtc);

        return notification;
    }

    public void MarkAsRead(DateTime nowUtc)
    {
        if (IsRead)
        {
            return; // idempotent : on garde la première date de lecture
        }

        ReadAtUtc = EnsureUtc(nowUtc);
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("La date doit être en UTC.", nameof(value));
}
