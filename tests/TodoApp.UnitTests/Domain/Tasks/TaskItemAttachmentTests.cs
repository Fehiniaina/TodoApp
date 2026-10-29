using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Attachments;

namespace TodoApp.UnitTests.Domain.Tasks;

public sealed class TaskItemAttachmentTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddAttachment_adds_it_and_raises_an_event()
    {
        var task = NewTask();

        var attachment = task.AddAttachment("devis.pdf", "application/pdf", 1_024, NewKey(), "alice", Now);

        Assert.Single(task.Attachments);
        var evt = Assert.IsType<TaskAttachmentAdded>(task.DomainEvents.Last());
        Assert.Equal(attachment.Id, evt.AttachmentId);
        Assert.Equal("devis.pdf", evt.FileName);
    }

    [Fact]
    public void Cannot_exceed_the_attachment_limit()
    {
        var task = NewTask();
        for (var i = 0; i < TaskItem.MaxAttachments; i++)
        {
            task.AddAttachment($"f{i}.txt", "text/plain", 10, NewKey(), "alice", Now);
        }

        Assert.Throws<InvalidOperationException>(() =>
            task.AddAttachment("one-too-many.txt", "text/plain", 10, NewKey(), "alice", Now));
    }

    [Fact]
    public void RemoveAttachment_returns_it_and_raises_an_event()
    {
        var task = NewTask();
        var added = task.AddAttachment("devis.pdf", "application/pdf", 1_024, NewKey(), "alice", Now);

        var removed = task.RemoveAttachment(added.Id, "alice", Now);

        Assert.Same(added, removed);
        Assert.Empty(task.Attachments);
        Assert.IsType<TaskAttachmentRemoved>(task.DomainEvents.Last());
    }

    [Fact]
    public void Oversized_attachment_is_rejected_by_the_domain()
    {
        var task = NewTask();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            task.AddAttachment("big.zip", "application/zip", TaskAttachment.MaxSizeBytes + 1, NewKey(), "alice", Now));
    }

    private static TaskItem NewTask() =>
        TaskItem.Create("alice", "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);

    private static string NewKey() => $"2026/10/{Guid.NewGuid():N}";
}
