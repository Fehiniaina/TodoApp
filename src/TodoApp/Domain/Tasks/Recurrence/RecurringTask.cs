namespace TodoApp.Domain.Tasks.Recurrence;

internal sealed class RecurringTask
{
    private RecurringTask() { }   // EF Core

    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public RecurrenceRule Rule { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string TimeZoneId { get; private set; } = null!;
    public DateOnly? NextOccurrence { get; private set; }
    public bool IsStopped { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public TaskType Type { get; private set; }

    public static RecurringTask Create(
        string ownerId, string title, string? description, TaskPriority priority,
        RecurrenceRule rule, DateOnly startDate, DateOnly? endDate,
        string timeZoneId, DateTime createdAtUtc, TaskType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        }

        var series = new RecurringTask
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Priority = priority,
            Rule = rule,
            StartDate = startDate,
            EndDate = endDate,
            TimeZoneId = timeZoneId,
            CreatedAtUtc = createdAtUtc,
            Type = EnsureType(type),
        };

        series.NextOccurrence = series.WithinEnd(rule.NextAfter(startDate, startDate.AddDays(-1)));
        return series;
    }

    /// <summary>
    /// Renvoie la date de l'occurrence à créer pour <paramref name="today"/> (date locale de la série),
    /// ou null si rien n'est dû. Les occurrences manquées sont sautées : seule la plus récente est créée.
    /// </summary>
    public DateOnly? CollectDueOccurrence(DateOnly today)
    {
        if (IsStopped || NextOccurrence is not { } due || due > today)
        {
            return null;
        }

        var latest = due;
        var next = WithinEnd(Rule.NextAfter(StartDate, latest));

        while (next is { } candidate && candidate <= today)
        {
            latest = candidate;
            next = WithinEnd(Rule.NextAfter(StartDate, candidate));
        }

        NextOccurrence = next;
        return latest;
    }

    public TaskItem CreateOccurrence(DateOnly dueDate) =>
        TaskItem.CreateOccurrence(OwnerId, Title, Description, Priority, dueDate, Id, CreatedAtUtc, Type);

    public void Stop()
    {
        IsStopped = true;
        NextOccurrence = null;
    }

    private DateOnly? WithinEnd(DateOnly date) =>
        EndDate is { } end && date > end ? null : date;

    private static TaskType EnsureType(TaskType type) =>
        Enum.IsDefined(type)
        ? type
        : throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown TaskType value.");
}
