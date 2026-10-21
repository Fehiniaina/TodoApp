using TodoApp.Domain.Common;
using TodoApp.Domain.Tasks.Events;
using TodoApp.Domain.Tasks.Exceptions;

namespace TodoApp.Domain.Tasks;

internal sealed class TaskItem : AggregateRoot
{
    // Required by EF Core for materialization.
    private TaskItem()
    {
    }

    private readonly List<ChecklistItem> _checklist = [];

    public Guid Id { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public TaskItemStatus Status { get; private set; }

    /// <summary>Calculée, plus stockée : "terminée" = statut Résolue.</summary>
    public bool IsCompleted => Status == TaskItemStatus.Resolved;

    /// <summary>Statuts vers lesquels la tâche peut passer (alimente le menu de l'interface).</summary>
    public IReadOnlySet<TaskItemStatus> AllowedNextStatuses => TaskWorkflow.NextStatuses(Status);


    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public const int MaxChecklistItems = 50;

    /// <summary>Vue en lecture seule, sans copie. L'ordre n'est pas garanti (ordre de chargement EF).</summary>
    public IReadOnlyCollection<ChecklistItem> Checklist => _checklist.AsReadOnly();

    public Guid? RecurringTaskId { get; private set; }

    /// <summary>Étapes triées par position (alloue une nouvelle liste).</summary>
    public IReadOnlyList<ChecklistItem> GetOrderedChecklist() =>
        [.. _checklist.OrderBy(i => i.Position)];

    public static TaskItem Create(
        string createdBy,
        string title,
        string? description,
        TaskPriority priority,
        DateOnly? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        return new TaskItem
        {
            Id = Guid.CreateVersion7(),
            CreatedById = createdBy,
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

    public void ChangeStatus(TaskItemStatus newStatus, string actorId, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, "Unknown task status.");
        }

        // Npgsql refuse d'écrire un DateTime non UTC dans une colonne timestamptz : on échoue tôt et clairement.
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC.", nameof(nowUtc));
        }

        // Idempotent : même statut → aucun changement, aucune ligne d'historique.
        if (newStatus == Status)
        {
            return;
        }

        if (!TaskWorkflow.CanMove(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        var previous = Status;
        Status = newStatus;

        // Entrée en "Résolue" : on date la résolution. Sortie (réouverture) : on efface la date.
        CompletedAt = newStatus == TaskItemStatus.Resolved ? nowUtc : null;

        Raise(new TaskStatusChanged(Id, actorId, previous, newStatus, nowUtc));
    }

    public static TaskItem CreateOccurrence(
    string createdBy, string title, string? description, TaskPriority priority,
    DateOnly dueDate, Guid recurringTaskId)
    {
        var task = Create(createdBy, title, description, priority, dueDate);
        task.RecurringTaskId = recurringTaskId;
        return task;
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
