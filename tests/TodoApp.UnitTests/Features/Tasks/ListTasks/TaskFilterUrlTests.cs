using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.ListTasks;

namespace TodoApp.UnitTests.Features.Tasks.ListTasks;

public sealed class TaskFilterUrlTests
{
    [Fact]
    public void Parses_all_criteria()
    {
        var filter = TaskFilterUrl.Parse(
            ["High", "urgent"], ["user-1", "none"], "range", "2026-11-01", "2026-11-30");

        Assert.True(filter.Priorities.SetEquals([TaskPriority.High, TaskPriority.Urgent]));
        Assert.True(filter.AssigneeIds.SetEquals(["user-1"]));
        Assert.True(filter.IncludeUnassigned);
        Assert.Equal(DueDateFilter.Range, filter.DueDate);
        Assert.Equal(new DateOnly(2026, 11, 1), filter.DueFrom);
        Assert.Equal(new DateOnly(2026, 11, 30), filter.DueTo);

        // Un critère par assertion : en cas d'échec, on sait lequel ne compte pas.
        Assert.True(filter.FiltersPriority);
        Assert.True(filter.FiltersAssignee);
        Assert.True(filter.HasDueDateFilter);
        Assert.Equal(3, filter.ActiveCount);
    }

    [Fact]
    public void Unknown_values_are_ignored()
    {
        var filter = TaskFilterUrl.Parse(["Haute", "999"], [""], "demain", "pas-une-date", null);

        Assert.True(filter.IsEmpty);
    }

    [Fact]
    public void Range_dates_are_ignored_without_range_mode()
    {
        var filter = TaskFilterUrl.Parse(null, null, "overdue", "2026-11-01", null);

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
            IncludeUnassigned = true,
            DueDate = DueDateFilter.Next7Days,
        };

        var parsed = RoundTrip(original);

        Assert.Equal(original, parsed); // égalité par valeur du record
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
    public void Empty_filter_removes_every_parameter()
    {
        var parameters = TaskFilterUrl.ToQueryParameters(TaskFilter.None);

        Assert.All(parameters.Values, Assert.Null);
    }

    private static TaskFilter RoundTrip(TaskFilter filter)
    {
        var p = TaskFilterUrl.ToQueryParameters(filter);

        return TaskFilterUrl.Parse(
            (string[]?)p[TaskFilterUrl.PriorityParam],
            (string[]?)p[TaskFilterUrl.AssigneeParam],
            (string?)p[TaskFilterUrl.DueParam],
            (string?)p[TaskFilterUrl.DueFromParam],
            (string?)p[TaskFilterUrl.DueToParam]);
    }
}
