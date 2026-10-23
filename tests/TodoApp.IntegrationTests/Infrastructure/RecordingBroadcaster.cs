using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Features.Notifications.Realtime;

namespace TodoApp.IntegrationTests.Infrastructure;

internal sealed class RecordingBroadcaster : INotificationBroadcaster
{
    public List<string> Published { get; } = [];

    public IDisposable Subscribe(string userId, Func<Task> onNotified) => throw new NotSupportedException();

    public void Publish(string userId) => Published.Add(userId);
}
