using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Filtering.Specifications;
using TodoApp.Features.Tasks.ListTasks;

namespace TodoApp.UnitTests.Features.Tasks.Filtering;

public sealed class DueDateSpecificationTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);

    [Theory]
    [InlineData(-30, true)]
    [InlineData(-1, true)]
    [InlineData(0, false)]   // échéance aujourd'hui : pas encore en retard
    [InlineData(1, false)]
    public void Overdue_matches_strictly_past_due_dates(int offset, bool expected)
    {
        var spec = new DueDateSpecification(DueDateFilter.Overdue, Today);

        Assert.Equal(expected, spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(offset))));
    }

    [Fact]
    public void Overdue_excludes_tasks_without_due_date()
    {
        var spec = new DueDateSpecification(DueDateFilter.Overdue, Today);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: null)));
    }

    [Theory]
    [InlineData(TaskItemStatus.Resolved)]
    public void Overdue_excludes_closed_tasks(TaskItemStatus closedStatus)
    {
        var spec = new DueDateSpecification(DueDateFilter.Overdue, Today);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(-3), status: closedStatus)));
    }

    [Theory]
    [InlineData(TaskItemStatus.ToDo)]
    [InlineData(TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InValidation)]
    public void Overdue_includes_open_tasks(TaskItemStatus openStatus)
    {
        var spec = new DueDateSpecification(DueDateFilter.Overdue, Today);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(-3), status: openStatus)));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Today_matches_only_current_day(int offset, bool expected)
    {
        var spec = new DueDateSpecification(DueDateFilter.Today, Today);

        Assert.Equal(expected, spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(offset))));
    }

    [Fact]
    public void Today_excludes_tasks_without_due_date()
    {
        var spec = new DueDateSpecification(DueDateFilter.Today, Today);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: null)));
    }

    [Theory]
    [InlineData(-1, false)]  // en retard : pas « à venir »
    [InlineData(0, true)]    // aujourd'hui inclus
    [InlineData(6, true)]    // dernier jour de la fenêtre
    [InlineData(7, false)]   // juste après
    public void Next7Days_covers_today_to_day_plus_six(int offset, bool expected)
    {
        var spec = new DueDateSpecification(DueDateFilter.Next7Days, Today);

        Assert.Equal(expected, spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(offset))));
    }

    [Fact]
    public void Next7Days_window_size_is_seven_days()
    {
        Assert.Equal(7, DueDateSpecification.UpcomingWindowDays);
    }

    [Fact]
    public void Next7Days_excludes_tasks_without_due_date()
    {
        var spec = new DueDateSpecification(DueDateFilter.Next7Days, Today);

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: null)));
    }

    [Fact]
    public void NoDueDate_matches_only_tasks_without_due_date()
    {
        var spec = new DueDateSpecification(DueDateFilter.NoDueDate, Today);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(dueDate: null)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today)));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]    // borne basse incluse
    [InlineData(3, true)]
    [InlineData(5, true)]    // borne haute incluse
    [InlineData(6, false)]
    public void Range_is_inclusive_on_both_bounds(int offset, bool expected)
    {
        var spec = new DueDateSpecification(DueDateFilter.Range, Today, from: Today, to: Today.AddDays(5));

        Assert.Equal(expected, spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(offset))));
    }

    [Fact]
    public void Range_with_single_day_matches_that_day_only()
    {
        var spec = new DueDateSpecification(DueDateFilter.Range, Today, from: Today, to: Today);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today)));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(1))));
    }

    [Fact]
    public void Range_with_only_lower_bound_is_open_ended()
    {
        var spec = new DueDateSpecification(DueDateFilter.Range, Today, from: Today);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddYears(1))));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(-1))));
    }

    [Fact]
    public void Range_with_only_upper_bound_is_open_ended()
    {
        var spec = new DueDateSpecification(DueDateFilter.Range, Today, to: Today);

        Assert.True(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddYears(-1))));
        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: Today.AddDays(1))));
    }

    [Fact]
    public void Range_excludes_tasks_without_due_date()
    {
        var spec = new DueDateSpecification(DueDateFilter.Range, Today, from: Today.AddDays(-10), to: Today.AddDays(10));

        Assert.False(spec.IsSatisfiedBy(TestTasks.Create(dueDate: null)));
    }


    [Fact]
    public void Any_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DueDateSpecification(DueDateFilter.Any, Today));
    }

    [Fact]
    public void Undefined_mode_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DueDateSpecification((DueDateFilter)99, Today));
    }

    [Fact]
    public void Range_without_bounds_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new DueDateSpecification(DueDateFilter.Range, Today));
    }

    [Fact]
    public void Range_with_inverted_bounds_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => new DueDateSpecification(DueDateFilter.Range, Today, from: Today, to: Today.AddDays(-1)));
    }
}
