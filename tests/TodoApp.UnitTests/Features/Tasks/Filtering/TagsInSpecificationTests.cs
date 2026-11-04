using TodoApp.Domain.Tags;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Filtering.Specifications;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

public sealed class TagsInSpecificationTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Tag Frontend = Tag.Create("Frontend", TagColor.Blue, "owner", Now);
    private static readonly Tag Backend = Tag.Create("Backend", TagColor.Green, "owner", Now);
    private static readonly Tag Client = Tag.Create("Client", TagColor.Red, "owner", Now);

    [Fact]
    public void Matches_task_with_any_of_the_tags()
    {
        var spec = new TagsInSpecification([Frontend.Id, Backend.Id]);

        Assert.True(spec.IsSatisfiedBy(TaskWith(Frontend)));
        Assert.True(spec.IsSatisfiedBy(TaskWith(Backend)));
        Assert.True(spec.IsSatisfiedBy(TaskWith(Frontend, Backend)));
    }

    [Fact]
    public void Rejects_task_without_any_of_the_tags()
    {
        var spec = new TagsInSpecification([Frontend.Id]);

        Assert.False(spec.IsSatisfiedBy(TaskWith(Client)));
        Assert.False(spec.IsSatisfiedBy(TaskWith()));
    }

    private static TaskItem TaskWith(params Tag[] tags)
    {
        var task = TaskItem.Create("owner", "Tâche", null, TaskPriority.Medium, null, Now, TaskType.Task);
        foreach (var tag in tags)
        {
            task.AddTag(tag, "owner", Now);
        }

        return task;
    }
}
