using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;

namespace TodoApp.UnitTests.Domain.Tasks;

public sealed class TaskTypeInvariantsTests
{
    private const string Owner = "owner-id";
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Start = new(2026, 10, 1);

    [Fact]
    public void Occurrence_inherits_the_type_of_its_series()
    {
        foreach (var type in Enum.GetValues<TaskType>())
        {
            var series = NewSeries(type);

            var occurrence = series.CreateOccurrence(Start);

            Assert.Equal(type, occurrence.Type);
            Assert.Equal(series.Id, occurrence.RecurringTaskId);
        }
    }

    [Fact]
    public void Bug_occurrence_stays_bug_despite_zero_value()
    {
        // Rappel du piège : Bug = 0 = default(TaskType). Une occurrence qui « oublierait »
        // le type serait aussi Bug ; on vérifie donc avec un AUTRE type que le défaut.
        var series = NewSeries(TaskType.UserStory);

        Assert.Equal(TaskType.UserStory, series.CreateOccurrence(Start).Type);
    }

    [Fact]
    public void Series_rejects_undefined_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewSeries((TaskType)99));
    }

    [Fact]
    public void Create_rejects_undefined_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, CreatedAtUtc, (TaskType)99));
    }

    [Fact]
    public void CreateOccurrence_rejects_undefined_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaskItem.CreateOccurrence(Owner, "Tâche", null, TaskPriority.Medium, Start, Guid.NewGuid(), CreatedAtUtc, (TaskType)99));
    }

    [Fact]
    public void ChangeType_rejects_undefined_type()
    {
        var task = TaskItem.Create(Owner, "Tâche", null, TaskPriority.Medium, null, CreatedAtUtc, TaskType.Task);

        Assert.Throws<ArgumentOutOfRangeException>(() => task.ChangeType((TaskType)99, Owner, CreatedAtUtc));
        Assert.Equal(TaskType.Task, task.Type); // inchangé après le refus
    }

    private static RecurringTask NewSeries(TaskType type) =>
        RecurringTask.Create(Owner, "Série", null, TaskPriority.Medium, DailyRule(), Start,
            endDate: null, timeZoneId: "Indian/Mauritius", CreatedAtUtc, type);

    private static RecurrenceRule DailyRule() => RecurrenceRule.Daily();
}
