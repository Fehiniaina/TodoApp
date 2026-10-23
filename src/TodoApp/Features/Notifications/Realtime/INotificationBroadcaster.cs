namespace TodoApp.Features.Notifications.Realtime;

internal interface INotificationBroadcaster
{
    IDisposable Subscribe(string userId, Func<Task> onNotified);

    void Publish(string userId);
}
