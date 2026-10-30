using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;

namespace TodoApp.UnitTests.Features.Tasks.ListTasks;

public sealed class TaskFilterUrlTests
{
    [Fact]
    public void Parses_all_criteria()
    {
        var filter = TaskFilterUrl.Parse(
            ["High", "urgent"], ["user-1", "none"], ["Bug", "analyse", "Task"], "range", "2026-11-01", "2026-11-30");

        Assert.True(filter.Priorities.SetEquals([TaskPriority.High, TaskPriority.Urgent]));
        Assert.True(filter.AssigneeIds.SetEquals(["user-1"]));
        Assert.True(filter.IncludeUnassigned);
        Assert.True(filter.Types.SetEquals([TaskType.Bug, TaskType.Analyse, TaskType.Task]));
        Assert.Equal(DueDateFilter.Range, filter.DueDate);
        Assert.Equal(new DateOnly(2026, 11, 1), filter.DueFrom);
        Assert.Equal(new DateOnly(2026, 11, 30), filter.DueTo);

        // Un critère par assertion : en cas d'échec, on sait lequel ne compte pas.
        Assert.True(filter.FiltersPriority);
        Assert.True(filter.FiltersAssignee);
        Assert.True(filter.FiltersType);
        Assert.True(filter.HasDueDateFilter);
        Assert.Equal(4, filter.ActiveCount);
    }

    [Fact]
    public void Unknown_values_are_ignored()
    {
        var filter = TaskFilterUrl.Parse(["Haute", "999"], [""], ["Epic", ""], "demain", "pas-une-date", null);

        Assert.True(filter.IsEmpty);
    }

    [Theory]
    [InlineData("0")]   // valeur numérique de Bug : refusée, on n'accepte que les noms
    [InlineData("42")]
    [InlineData("Epic")]
    [InlineData("Tâche")] // libellé affiché : pas un nom de membre
    public void Unknown_or_numeric_types_are_ignored(string value)
    {
        var filter = TaskFilterUrl.Parse(null, null, [value], null, null, null);

        Assert.False(filter.FiltersType);
    }

    [Fact]
    public void Range_dates_are_ignored_without_range_mode()
    {
        var filter = TaskFilterUrl.Parse(null, null, null, "overdue", "2026-11-01", null);

        Assert.Equal(DueDateFilter.Overdue, filter.DueDate);
        Assert.Null(filter.DueFrom);
    }

    [Fact]
    public void Query_parameters_round_trip()
    {
        var original = new TaskFilter
        {
            Priorities = new HashSet<TaskPriority> { TaskPriority.Low },
            AssigneeIds = new HashSet<string>(StringComparer.Ordinal) { "user-1" },
            Types = new HashSet<TaskType> { TaskType.UserStory },
            IncludeUnassigned = true,
            DueDate = DueDateFilter.Next7Days,
        };

        Assert.Equal(original, RoundTrip(original)); // égalité par valeur du record
    }

    [Fact]
    public void Range_round_trip_keeps_bounds()
    {
        var original = new TaskFilter
        {
            DueDate = DueDateFilter.Range,
            DueFrom = new DateOnly(2026, 11, 1),
            DueTo = new DateOnly(2026, 11, 30),
        };

        Assert.Equal(original, RoundTrip(original));
    }

    [Fact]
    public void Type_round_trip_keeps_types()
    {
        var original = new TaskFilter { Types = new HashSet<TaskType> { TaskType.UserStory, TaskType.Bug } };

        var parsed = RoundTrip(original);

        // Assertion directe : ne dépend pas du Equals du record (qui pourrait oublier les types).
        Assert.True(parsed.Types.SetEquals([TaskType.UserStory, TaskType.Bug]));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void Types_are_written_sorted_for_a_canonical_url()
    {
        var a = TaskFilterUrl.ToQueryParameters(new TaskFilter { Types = new HashSet<TaskType> { TaskType.UserStory, TaskType.Bug } });
        var b = TaskFilterUrl.ToQueryParameters(new TaskFilter { Types = new HashSet<TaskType> { TaskType.Bug, TaskType.UserStory } });

        Assert.Equal((string[]?)a[TaskFilterUrl.TypeParam], (string[]?)b[TaskFilterUrl.TypeParam]);
    }

    [Fact]
    public void Empty_filter_removes_every_parameter()
    {
        var parameters = TaskFilterUrl.ToQueryParameters(TaskFilter.None);

        Assert.All(parameters.Values, Assert.Null);
    }

    [Theory]
    [InlineData("Story")]  // favori créé avant le renommage Story → UserStory
    [InlineData("story")]
    [InlineData("STORY")]
    public void Legacy_type_link_still_works(string value)
    {
        var filter = TaskFilterUrl.Parse(null, null, [value], null, null, null);

        Assert.True(filter.Types.SetEquals([TaskType.UserStory]));
    }

    [Fact]
    public void Url_uses_slugs_not_member_names()
    {
        var parameters = TaskFilterUrl.ToQueryParameters(
            new TaskFilter { Types = new HashSet<TaskType> { TaskType.UserStory } });

        var types = Assert.IsType<string[]>(parameters[TaskFilterUrl.TypeParam]);
        Assert.Equal(["story"], types);
    }

    [Fact]
    public void Ignored_values_are_reported()
    {
        _ = TaskFilterUrl.Parse(["high", "Haute"], null, ["bug", "Epic"], "demain", null, null, out var ignored);

        Assert.Equal(["Haute", "Epic", "demain"], ignored);
    }

    [Fact]
    public void Valid_link_reports_nothing()
    {
        _ = TaskFilterUrl.Parse(["high"], ["none"], ["story"], "overdue", null, null, out var ignored);

        Assert.Empty(ignored);
    }

    private static TaskFilter RoundTrip(TaskFilter filter)
    {
        var p = TaskFilterUrl.ToQueryParameters(filter);

        return TaskFilterUrl.Parse(
            (string[]?)p[TaskFilterUrl.PriorityParam],
            (string[]?)p[TaskFilterUrl.AssigneeParam],
            (string[]?)p[TaskFilterUrl.TypeParam],
            (string?)p[TaskFilterUrl.DueParam],
            (string?)p[TaskFilterUrl.DueFromParam],
            (string?)p[TaskFilterUrl.DueToParam]);
    }
}
