namespace TodoApp.Domain.Tasks.Recurrence;

public sealed record RecurrenceRule
{
    private const int MaxInterval = 365;

    private RecurrenceRule() { }   // EF Core

    private RecurrenceRule(RecurrenceFrequency frequency, int interval, WeekDays daysOfWeek, int? dayOfMonth)
    {
        if (interval is < 1 or > MaxInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, $"Interval must be between 1 and {MaxInterval}.");
        }

        Frequency = frequency;
        Interval = interval;
        DaysOfWeek = daysOfWeek;
        DayOfMonth = dayOfMonth;
    }

    public RecurrenceFrequency Frequency { get; private set; }
    public int Interval { get; private set; }
    public WeekDays DaysOfWeek { get; private set; }
    public int? DayOfMonth { get; private set; }

    public static RecurrenceRule Daily(int interval = 1) =>
        new(RecurrenceFrequency.Daily, interval, WeekDays.None, null);

    public static RecurrenceRule Weekly(WeekDays days, int interval = 1) =>
        days == WeekDays.None
            ? throw new ArgumentException("At least one day of the week is required.", nameof(days))
            : new(RecurrenceFrequency.Weekly, interval, days, null);

    public static RecurrenceRule Monthly(int dayOfMonth, int interval = 1) =>
        dayOfMonth is < 1 or > 31
            ? throw new ArgumentOutOfRangeException(nameof(dayOfMonth), dayOfMonth, "Day of month must be between 1 and 31.")
            : new(RecurrenceFrequency.Monthly, interval, WeekDays.None, dayOfMonth);

    /// <summary>
    /// Première occurrence strictement après <paramref name="after"/>, jamais avant <paramref name="anchor"/>
    /// (date de début de la série, qui sert de référence pour les intervalles « toutes les N semaines »).
    /// </summary>
    public DateOnly NextAfter(DateOnly anchor, DateOnly after)
    {
        var from = after < anchor ? anchor : after.AddDays(1);

        return Frequency switch
        {
            RecurrenceFrequency.Daily => NextDaily(anchor, from),
            RecurrenceFrequency.Weekly => NextWeekly(anchor, from),
            RecurrenceFrequency.Monthly => NextMonthly(anchor, from),
            _ => throw new InvalidOperationException($"Unsupported frequency {Frequency}."),
        };
    }

    private DateOnly NextDaily(DateOnly anchor, DateOnly from)
    {
        var remainder = (from.DayNumber - anchor.DayNumber) % Interval;
        return remainder == 0 ? from : from.AddDays(Interval - remainder);
    }

    private DateOnly NextWeekly(DateOnly anchor, DateOnly from)
    {
        var anchorWeek = StartOfWeek(anchor);
        var maxIterations = 7 * (Interval + 1);   // garde-fou : une donnée corrompue ne doit pas boucler à l'infini

        for (var i = 0; i < maxIterations; i++)
        {
            var day = from.AddDays(i);
            var weekIndex = (StartOfWeek(day).DayNumber - anchorWeek.DayNumber) / 7;

            if (weekIndex % Interval == 0 && DaysOfWeek.HasFlag(ToWeekDay(day.DayOfWeek)))
            {
                return day;
            }
        }

        throw new InvalidOperationException("Weekly rule has no selected day.");
    }

    private DateOnly NextMonthly(DateOnly anchor, DateOnly from)
    {
        var targetDay = DayOfMonth ?? anchor.Day;
        var anchorMonth = MonthIndex(anchor);
        var fromMonth = MonthIndex(from);

        var offset = (fromMonth - anchorMonth) % Interval;
        var monthIndex = offset == 0 ? fromMonth : fromMonth + Interval - offset;

        var candidate = DateInMonth(monthIndex, targetDay);

        return candidate >= from ? candidate : DateInMonth(monthIndex + Interval, targetDay);
    }

    private static int MonthIndex(DateOnly date) => date.Year * 12 + date.Month - 1;

    private static DateOnly DateInMonth(int monthIndex, int day)
    {
        var year = monthIndex / 12;
        var month = monthIndex % 12 + 1;

        return new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
    }

    private static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static WeekDays ToWeekDay(DayOfWeek dayOfWeek) =>
        (WeekDays)(1 << (((int)dayOfWeek + 6) % 7));
}
