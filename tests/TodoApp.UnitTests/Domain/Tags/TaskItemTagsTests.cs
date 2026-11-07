using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Events;

namespace TodoApp.UnitTests.Domain.Tasks;

public sealed class TaskItemTagsTests
{
    private const string Actor = "actor-id";
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Adding_a_tag_records_it_and_raises_an_event_with_its_name()
    {
        var task = NewTask();
        var tag = NewTag("Frontend");

        Assert.True(task.AddTag(tag, Actor, Now));

        Assert.Equal(tag.Id, Assert.Single(task.Tags).TagId);
        var added = Assert.Single(task.DomainEvents.OfType<TaskTagAdded>());
        Assert.Equal("Frontend", added.TagName);
    }

    [Fact]
    public void Adding_the_same_tag_twice_is_a_no_op()
    {
        var task = NewTask();
        var tag = NewTag("Frontend");
        task.AddTag(tag, Actor, Now);

        Assert.False(task.AddTag(tag, Actor, Now));

        Assert.Single(task.Tags);
        Assert.Single(task.DomainEvents.OfType<TaskTagAdded>());
    }

    [Fact]
    public void Cannot_exceed_the_maximum_number_of_tags()
    {
        var task = NewTask();
        for (var i = 0; i < TaskItem.MaxTags; i++)
        {
            task.AddTag(NewTag($"Tag {i}"), Actor, Now);
        }

        Assert.Throws<InvalidOperationException>(() => task.AddTag(NewTag("One too many"), Actor, Now));
        Assert.Equal(TaskItem.MaxTags, task.Tags.Count);
    }

    [Fact]
    public void Removing_a_tag_raises_an_event()
    {
        var task = NewTask();
        var tag = NewTag("Frontend");
        task.AddTag(tag, Actor, Now);

        Assert.True(task.RemoveTag(tag, Actor, Now));

        Assert.Empty(task.Tags);
        Assert.Equal("Frontend", Assert.Single(task.DomainEvents.OfType<TaskTagRemoved>()).TagName);
    }

    [Fact]
    public void Removing_an_absent_tag_is_a_no_op()
    {
        var task = NewTask();

        Assert.False(task.RemoveTag(NewTag("Frontend"), Actor, Now));
        Assert.Empty(task.DomainEvents.OfType<TaskTagRemoved>());
    }

    private static TaskItem NewTask() =>
        TaskItem.Create("owner", "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);

    private static Tag NewTag(string name) => Tag.Create(name, TagColor.Blue, "owner", Now);
}
