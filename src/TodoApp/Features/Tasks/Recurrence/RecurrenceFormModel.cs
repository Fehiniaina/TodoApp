using TodoApp.Domain.Tasks.Recurrence;

namespace TodoApp.Features.Tasks.Recurrence;

public sealed class RecurrenceFormModel
{
    public RepeatOption Repeat { get; set; } = RepeatOption.None;
    public int Interval { get; set; } = 1;
    public WeekDays Days { get; set; } = WeekDays.None;
    public int? DayOfMonth { get; set; }

    public RecurrenceFrequency ToFrequency() => Repeat switch
    {
        RepeatOption.Daily => RecurrenceFrequency.Daily,
        RepeatOption.Weekly => RecurrenceFrequency.Weekly,
        RepeatOption.Monthly => RecurrenceFrequency.Monthly,
        _ => throw new InvalidOperationException("No recurrence selected."),
    };
}
