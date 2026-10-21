using System;
using TodoApp.Domain.Tasks;

namespace TodoApp.IntegrationTests.Features.Tasks.CreateTask;

public class TaskItemChecklistTests
{
    private static TaskItem NewTask() => TaskItem.Create("owner", "Task", null, TaskPriority.Medium, null);

    [Fact]
    public void RemoveChecklistItem_ReindexesPositions()
    {
        var task = NewTask();
        var first = task.AddChecklistItem("A");
        task.AddChecklistItem("B");
        task.AddChecklistItem("C");

        task.RemoveChecklistItem(first.Id);

        Assert.Equal(["B", "C"], task.Checklist.Select(i => i.Title));
        Assert.Equal([0, 1], task.Checklist.Select(i => i.Position));
    }

    [Fact]
    public void AddChecklistItem_BeyondLimit_Throws()
    {
        var task = NewTask();
        for (var i = 0; i < TaskItem.MaxChecklistItems; i++) task.AddChecklistItem($"Step {i}");

        Assert.Throws<InvalidOperationException>(() => task.AddChecklistItem("One too many"));
    }
}
