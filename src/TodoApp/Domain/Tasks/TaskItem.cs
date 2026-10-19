namespace TodoApp.Domain.Tasks;

internal sealed class TaskItem
{
    // Required by EF Core for materialization.
    private TaskItem()
    {
    }

    private readonly List<ChecklistItem> _checklist = [];

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

    public const int MaxChecklistItems = 50;

    /// <summary>Vue en lecture seule, sans copie. L'ordre n'est pas garanti (ordre de chargement EF).</summary>
    public IReadOnlyCollection<ChecklistItem> Checklist => _checklist.AsReadOnly();

    /// <summary>Étapes triées par position (alloue une nouvelle liste).</summary>
    public IReadOnlyList<ChecklistItem> GetOrderedChecklist() =>
        [.. _checklist.OrderBy(i => i.Position)];

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

    public ChecklistItem AddChecklistItem(string title)
    {
        if (_checklist.Count >= MaxChecklistItems)
        {
            throw new InvalidOperationException($"A task cannot have more than {MaxChecklistItems} checklist items.");
        }

        var item = new ChecklistItem(Guid.CreateVersion7(), NormalizeTitle(title), _checklist.Count);
        _checklist.Add(item);
        return item;
    }

    public bool SetChecklistItemDone(Guid itemId, bool isDone)
    {
        var item = _checklist.Find(i => i.Id == itemId);
        if (item is null) return false;

        item.SetDone(isDone);
        return true;
    }

    public bool RemoveChecklistItem(Guid itemId)
    {
        var item = _checklist.Find(i => i.Id == itemId);
        if (item is null) return false;

        _checklist.Remove(item);

        // Positions contiguës (0, 1, 2…) après suppression.
        var ordered = _checklist.OrderBy(i => i.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].MoveTo(i);
        }

        return true;
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
