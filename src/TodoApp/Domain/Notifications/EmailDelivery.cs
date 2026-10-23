namespace TodoApp.Domain.Notifications;

internal sealed class EmailDelivery
{
    public const int MaxErrorLength = 500;

    // EF Core
    private EmailDelivery()
    {
    }

    private EmailDelivery(DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Status = EmailDeliveryStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc; // à envoyer dès le prochain cycle
    }

    public Guid Id { get; private set; }
    public Guid NotificationId { get; }
    public Notification Notification { get; private set; } = null!;
    public EmailDeliveryStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public string? LastError { get; private set; }

    internal static EmailDelivery Schedule(DateTime createdAtUtc) => new(createdAtUtc);

    public void MarkSent(DateTime nowUtc)
    {
        EnsurePending();
        Attempts++;
        Status = EmailDeliveryStatus.Sent;
        SentAtUtc = nowUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        EnsurePending();
        Attempts++;
        Status = EmailDeliveryStatus.Failed;
        LastError = Truncate(error);
    }

    public void ScheduleRetry(DateTime nowUtc, string error, int maxAttempts, TimeSpan maxBackoff)
    {
        EnsurePending();
        Attempts++;
        LastError = Truncate(error);

        if (Attempts >= maxAttempts)
        {
            Status = EmailDeliveryStatus.Failed;
            return;
        }

        // 2, 4, 8, 16, 32 min… plafonné
        var backoff = TimeSpan.FromMinutes(Math.Pow(2, Attempts));
        NextAttemptAtUtc = nowUtc + (backoff < maxBackoff ? backoff : maxBackoff);
    }

    public void Expire()
    {
        EnsurePending();
        Status = EmailDeliveryStatus.Expired;
    }

    private void EnsurePending()
    {
        if (Status != EmailDeliveryStatus.Pending)
        {
            throw new InvalidOperationException($"L'envoi {Id} n'est plus en attente ({Status}).");
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
