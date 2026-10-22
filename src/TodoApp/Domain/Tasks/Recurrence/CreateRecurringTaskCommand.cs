namespace TodoApp.Domain.Tasks.Recurrence;

internal sealed record CreateRecurringTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    RecurrenceFrequency Frequency,
    int Interval,
    WeekDays DaysOfWeek,
    int? DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate);
