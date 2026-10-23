using TodoApp.Features.Notifications.Email;

namespace TodoApp.UnitTests.Features.Notifications;

public sealed class EmailDeliverySignalTests
{
    [Fact]
    public async Task Notify_wakes_the_waiter_immediately()
    {
        var signal = new EmailDeliverySignal();
        signal.Notify();

        var wait = signal.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        var first = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(wait, first);
    }

    [Fact]
    public async Task Several_notifications_collapse_into_a_single_wake_up()
    {
        var signal = new EmailDeliverySignal();
        signal.Notify();
        signal.Notify();
        signal.Notify();

        // Le premier appel consomme l'unique signal en attente.
        await signal
            .WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(1));

        // Le second ne trouve plus de signal : il se termine par son propre délai (100 ms).
        var started = TimeProvider.System.GetTimestamp();
        await signal.WaitAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        var elapsed = TimeProvider.System.GetElapsedTime(started);

        Assert.True(elapsed >= TimeSpan.FromMilliseconds(90), $"Réveil trop rapide ({elapsed.TotalMilliseconds} ms) : un second signal était en attente.");
    }

    [Fact]
    public async Task Cancellation_stops_waiting()
    {
        var signal = new EmailDeliverySignal();
        using var cts = new CancellationTokenSource();

        var wait = signal.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
