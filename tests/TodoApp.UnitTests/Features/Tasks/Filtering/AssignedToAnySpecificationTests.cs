using TodoApp.Features.Tasks.Filtering.Specifications;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

public sealed class AssignedToAnySpecificationTests
{
    private const string Alice = "alice-id";
    private const string Bob = "bob-id";
    private const string Carol = "carol-id";

    [Fact]
    public void Matches_task_assigned_to_one_of_the_users()
    {
        var spec = new AssignedToAnySpecification([Alice, Bob], includeUnassigned: false);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Alice)));
        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Bob)));
    }

    [Fact]
    public void Rejects_task_assigned_to_another_user()
    {
        var spec = new AssignedToAnySpecification([Alice, Bob], includeUnassigned: false);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Carol)));
    }

    [Fact]
    public void Rejects_unassigned_task_when_not_included()
    {
        var spec = new AssignedToAnySpecification([Alice], includeUnassigned: false);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: null)));
    }

    [Fact]
    public void Only_unassigned_matches_unassigned_tasks_only()
    {
        var spec = new AssignedToAnySpecification([], includeUnassigned: true);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: null)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Alice)));
    }

    [Fact]
    public void Users_and_unassigned_are_combined_with_or()
    {
        var spec = new AssignedToAnySpecification([Alice], includeUnassigned: true);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Alice)));
        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: null)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Bob)));
    }

    [Fact]
    public void Ids_are_compared_case_sensitively()
    {
        // Les ids Identity sont des GUID en minuscules : pas d'égalité « approximative ».
        var spec = new AssignedToAnySpecification([Alice.ToUpperInvariant()], includeUnassigned: false);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Alice)));
    }

    [Fact]
    public void Duplicate_ids_are_tolerated()
    {
        var spec = new AssignedToAnySpecification([Alice, Alice], includeUnassigned: false);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(assigneeId: Alice)));
    }

    [Fact]
    public void Throws_when_no_criterion_given()
    {
        Assert.Throws<ArgumentException>(() => new AssignedToAnySpecification([], includeUnassigned: false));
    }

    [Fact]
    public void Throws_when_ids_are_null()
    {
        Assert.Throws<ArgumentNullException>(() => new AssignedToAnySpecification(null!, includeUnassigned: true));
    }
}
