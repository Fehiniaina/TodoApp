using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Filtering.Specifications;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

public sealed class TypeInSpecificationTests
{
    [Fact]
    public void Single_type_matches_only_that_type()
    {
        foreach (var selected in Enum.GetValues<TaskType>())
        {
            var spec = new TypeInSpecification([selected]);

            foreach (var type in Enum.GetValues<TaskType>())
            {
                Assert.Equal(type == selected, spec.IsSatisfiedBy(TestTasks.Create(type: type)));
            }
        }
    }

    [Fact]
    public void Several_types_are_combined_with_or()
    {
        var spec = new TypeInSpecification([TaskType.Bug, TaskType.UserStory]);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.Bug)));
        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.UserStory)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.Task)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.Extraction)));
    }

    [Fact]
    public void Bug_type_is_matched_despite_being_zero()
    {
        // Bug = 0 = default(TaskType) : rappel du piège 1, la valeur zéro doit être filtrée comme les autres.
        var spec = new TypeInSpecification([TaskType.Bug]);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.Bug)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(type: TaskType.Task)));
    }

    [Fact]
    public void Expression_does_not_depend_on_later_changes_to_the_source_collection()
    {
        var source = new List<TaskType> { TaskType.Bug };
        var spec = new TypeInSpecification(source);
        var expression = spec.ToExpression().Compile();

        source.Add(TaskType.UserStory);

        Assert.False(expression(TestTasks.Create(type: TaskType.UserStory)));
    }
}
