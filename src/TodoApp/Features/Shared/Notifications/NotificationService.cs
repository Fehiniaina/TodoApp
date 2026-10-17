namespace TodoApp.Features.Shared.Notifications;

public enum NotificationLevel
{
    Success,
    Info,
    Warning,
    Error,
}

public sealed record Notification(Guid Id, NotificationLevel Level, string Message, string? Title);

/// <summary>
/// Per-circuit notification store. Components raise notifications;
/// <c>NotificationHost</c> renders them.
/// </summary>
internal sealed class NotificationService
{
    private const int MaxVisible = 5;

    private readonly List<Notification> _items = [];
    private readonly Lock _lock = new();

    public event Action? Changed;

    public IReadOnlyList<Notification> Items
    {
        get { lock (_lock) return [.. _items]; }
    }

    public void Success(string message, string? title = null) => Show(NotificationLevel.Success, message, title);
    public void Info(string message, string? title = null) => Show(NotificationLevel.Info, message, title);
    public void Warning(string message, string? title = null) => Show(NotificationLevel.Warning, message, title);
    public void Error(string message, string? title = null) => Show(NotificationLevel.Error, message, title);

    public void Show(NotificationLevel level, string message, string? title = null, TimeSpan? duration = null)
    {
        var notification = new Notification(Guid.NewGuid(), level, message, title);

        lock (_lock)
        {
            _items.Add(notification);
            if (_items.Count > MaxVisible)
            {
                _items.RemoveAt(0);
            }
        }

        Changed?.Invoke();

        var delay = duration ?? DefaultDuration(level);
        if (delay > TimeSpan.Zero)
        {
            _ = DismissLaterAsync(notification.Id, delay);
        }
    }

    public void Dismiss(Guid id)
    {
        bool removed;
        lock (_lock)
        {
            removed = _items.RemoveAll(n => n.Id == id) > 0;
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    // Errors stay until the user closes them: a message that disappears before
    // it is read is an accessibility issue.
    private static TimeSpan DefaultDuration(NotificationLevel level) => level switch
    {
        NotificationLevel.Error => TimeSpan.Zero,
        NotificationLevel.Warning => TimeSpan.FromSeconds(8),
        _ => TimeSpan.FromSeconds(4),
    };

    private async Task DismissLaterAsync(Guid id, TimeSpan delay)
    {
        await Task.Delay(delay);
        Dismiss(id);
    }
}