namespace TodoApp.Domain.Tasks;

internal sealed class TaskItem
{
    // Required by EF Core for materialization.
    private TaskItem()
    {
    }

    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }


    public static TaskItem Create(
        string ownerId,
        string title,
        string? description,
        TaskPriority priority,
        DateOnly? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        return new TaskItem
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Title = NormalizeTitle(title),
            Description = NormalizeDescription(description),
            Priority = EnsureDefined(priority),
            CreatedAtUtc = DateTime.UtcNow,
            DueDate = dueDate
        };
    }

    public void Update(string title, string? description, TaskPriority priority, DateOnly? dueDate)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Priority = EnsureDefined(priority);
        DueDate = dueDate;
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

    public void MoveToTrash(DateTime deletedAtUtc)
    {
        if (IsDeleted) return;              // idempotent : on garde la date d'origine
        IsDeleted = true;
        DeletedAt = deletedAtUtc;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
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
