namespace TodoApp.Domain.Tasks;

internal sealed class TaskItem
{
    // Required by EF Core for materialization.
    private TaskItem()
    {
    }

    private TaskItem(Guid id, string ownerId, string title, string? description, TaskPriority priority, DateTime createdAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Priority = priority;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public bool IsCompleted { get; private set; } = default;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static TaskItem Create(string ownerId, string title, string? description, TaskPriority priority) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = title.Trim(),
            Description = NormalizeDescription(description),
            Priority = EnsureDefined(priority),
            CreatedAtUtc = DateTime.UtcNow
        };

    public void Update(string title, string? description, TaskPriority priority)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Priority = EnsureDefined(priority);
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (IsCompleted)
        {
            return; // idempotent : on garde la date d'origine
        }

        IsCompleted = true;
        CompletedAt = completedAtUtc;
    }

    public void Reopen()
    {
        if (!IsCompleted)
        {
            return;
        }

        IsCompleted = false;
        CompletedAt = null;
    }

    private static string NormalizeTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return title.Trim();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static TaskPriority EnsureDefined(TaskPriority priority) =>
    Enum.IsDefined(priority)
        ? priority
        : throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown task priority.");
}
