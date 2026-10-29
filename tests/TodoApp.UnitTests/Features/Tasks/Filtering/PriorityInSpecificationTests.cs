using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Filtering.Specifications;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

public sealed class PriorityInSpecificationTests
{
    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Urgent)]
    public void Single_priority_matches_only_that_priority(TaskPriority selected)
    {
        var spec = new PriorityInSpecification([selected]);

        foreach (var priority in Enum.GetValues<TaskPriority>())
        {
            Assert.Equal(priority == selected, spec.IsSatisfiedBy(TestTasks.Create(priority: priority)));
        }
    }

    [Fact]
    public void Several_priorities_are_combined_with_or()
    {
        var spec = new PriorityInSpecification([TaskPriority.High, TaskPriority.Urgent]);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(priority: TaskPriority.High)));
        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(priority: TaskPriority.Urgent)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(priority: TaskPriority.Low)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(priority: TaskPriority.Medium)));
    }

    [Fact]
    public void All_priorities_match_everything()
    {
        var spec = new PriorityInSpecification(Enum.GetValues<TaskPriority>());

        Assert.All(
            Enum.GetValues<TaskPriority>(),
            priority => Assert.True(spec.IsSatisfiedBy(TestTasks.Create(priority: priority))));
    }

    [Fact]
    public void Expression_does_not_depend_on_later_changes_to_the_source_collection()
    {
        // La spec copie la collection : modifier la source ensuite ne change pas le filtre.
        var source = new List<TaskPriority> { TaskPriority.High };
        var spec = new PriorityInSpecification(source);
        var expression = spec.ToExpression().Compile();

        source.Add(TaskPriority.Low);

        Assert.False(expression(TestTasks.Create(priority: TaskPriority.Low)));
    }
}
