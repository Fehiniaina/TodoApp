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
    public DateTime CreatedAtUtc { get; private set; }

    public static TaskItem Create(string ownerId, string title, string? description, TaskPriority priority) =>
        new(Guid.NewGuid(), ownerId, title, description, priority, DateTime.UtcNow);
}
