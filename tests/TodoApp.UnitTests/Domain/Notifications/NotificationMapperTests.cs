using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Notifications;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Events;

namespace TodoApp.IntegrationTests.Domain.Notifications;

public sealed class NotificationMapperTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TaskId = Guid.NewGuid();

    [Fact]
    public void Assignment_to_another_user_creates_notification_for_new_assignee()
    {
        var evt = new TaskAssigned(TaskId, "alice", PreviousAssigneeId: null, NewAssigneeId: "bob", Now);

        var notification = NotificationMapper.ToNotification(evt);

        Assert.NotNull(notification);
        Assert.Equal("bob", notification.RecipientId);
        Assert.Equal("alice", notification.ActorId);
        Assert.Equal(TaskId, notification.TaskId);
        Assert.Equal(NotificationType.TaskAssigned, notification.Type);
        Assert.Equal(Now, notification.CreatedAtUtc);
        Assert.False(notification.IsRead);
    }

    [Theory]
    [InlineData("alice", null, "alice")]   // auto-assignation
    [InlineData("alice", "bob", null)]     // désassignation
    [InlineData("alice", "bob", "bob")]    // même assigné
    public void No_notification_is_created(string actorId, string? previousId, string? newId)
    {
        var evt = new TaskAssigned(TaskId, actorId, previousId, newId, Now);

        Assert.Null(NotificationMapper.ToNotification(evt));
    }

    [Fact]
    public void Other_events_do_not_create_notifications()
    {
        var evt = new TaskCreated(TaskId, "alice", Now);

        Assert.Null(NotificationMapper.ToNotification(evt));
    }

    [Fact]
    public void MarkAsRead_is_idempotent_and_keeps_first_read_date()
    {
        var notification = Notification.ForTaskAssignment("bob", TaskId, "alice", Now);

        notification.MarkAsRead(Now.AddMinutes(5));
        notification.MarkAsRead(Now.AddMinutes(10));

        Assert.Equal(Now.AddMinutes(5), notification.ReadAtUtc);
    }

    [Fact]
    public void Imported_assignment_creates_notification_without_email()
    {
        var evt = new TaskAssigned(TaskId, "alice", null, "bob", Now, AssignmentOrigin.Import);

        var notification = NotificationMapper.ToNotification(evt);

        Assert.NotNull(notification);
        Assert.Null(notification.Email);
    }
}
