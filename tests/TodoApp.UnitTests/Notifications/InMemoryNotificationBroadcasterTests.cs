using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApp.Features.Notifications.Realtime;

namespace TodoApp.UnitTests.Notifications;

public sealed class InMemoryNotificationBroadcasterTests
{
    private readonly InMemoryNotificationBroadcaster _broadcaster =
        new(NullLogger<InMemoryNotificationBroadcaster>.Instance);

    private static readonly string[] _expected = new[] { "tab1", "tab2" };

    [Fact]
    public async Task Publish_notifies_all_subscriptions_of_the_user()
    {
        var calls = new ConcurrentBag<string>();
        using var tab1 = _broadcaster.Subscribe("bob", Record(calls, "tab1"));
        using var tab2 = _broadcaster.Subscribe("bob", Record(calls, "tab2"));

        _broadcaster.Publish("bob");
        await WaitUntilAsync(() => calls.Count == 2);

        Assert.Equivalent(_expected, calls);
    }

    [Fact]
    public async Task Publish_does_not_notify_other_users()
    {
        var calls = new ConcurrentBag<string>();
        using var carol = _broadcaster.Subscribe("carol", Record(calls, "carol"));

        _broadcaster.Publish("bob");
        await Task.Delay(50);

        Assert.Empty(calls);
    }

    [Fact]
    public async Task Disposed_subscription_is_no_longer_notified()
    {
        var calls = new ConcurrentBag<string>();
        var subscription = _broadcaster.Subscribe("bob", Record(calls, "tab1"));

        subscription.Dispose();
        subscription.Dispose(); // idempotent
        _broadcaster.Publish("bob");
        await Task.Delay(50);

        Assert.Empty(calls);
    }

    [Fact]
    public async Task Failing_subscriber_does_not_prevent_others()
    {
        var calls = new ConcurrentBag<string>();
        using var broken = _broadcaster.Subscribe("bob", () => throw new InvalidOperationException("boom"));
        using var healthy = _broadcaster.Subscribe("bob", Record(calls, "healthy"));

        _broadcaster.Publish("bob");
        await WaitUntilAsync(() => calls.Count == 1);

        Assert.Single(calls);
    }

    private static Func<Task> Record(ConcurrentBag<string> calls, string name) => () =>
    {
        calls.Add(name);
        return Task.CompletedTask;
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
