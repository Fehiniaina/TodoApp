using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace TodoApp.Features.Notifications.Realtime;

/// <summary>
/// Relaie les notifications entre circuits Blazor d'une même instance de l'application.
/// Pour plusieurs instances : implémentation Postgres LISTEN/NOTIFY ou Redis.
/// </summary>
internal sealed partial class InMemoryNotificationBroadcaster(
    ILogger<InMemoryNotificationBroadcaster> logger) : INotificationBroadcaster
{
    private readonly ConcurrentDictionary<string, ImmutableArray<Func<Task>>> _subscribers =
        new(StringComparer.Ordinal);

    public IDisposable Subscribe(string userId, Func<Task> onNotified)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(onNotified);

        _subscribers.AddOrUpdate(
            userId,
            _ => [onNotified],
            (_, handlers) => handlers.Add(onNotified));

        return new Subscription(() => Unsubscribe(userId, onNotified));
    }

    public void Publish(string userId)
    {
        if (!_subscribers.TryGetValue(userId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            _ = InvokeSafelyAsync(userId, handler);
        }
    }

    private async Task InvokeSafelyAsync(string userId, Func<Task> handler)
    {
        try
        {
            await handler();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            LogSubscriberFailed(logger, userId, ex);
        }
    }

    private void Unsubscribe(string userId, Func<Task> handler)
    {
        while (_subscribers.TryGetValue(userId, out var current))
        {
            var updated = current.Remove(handler);

            var done = updated.IsEmpty
                ? _subscribers.TryRemove(KeyValuePair.Create(userId, current))
                : _subscribers.TryUpdate(userId, updated, current);

            if (done)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Échec de la notification temps réel pour {UserId}")]
    private static partial void LogSubscriberFailed(ILogger logger, string userId, Exception exception);

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
