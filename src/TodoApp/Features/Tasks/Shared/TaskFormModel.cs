using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

// Public : utilisé comme [Parameter] d'un composant public.
public sealed class TaskFormModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateOnly? DueDate { get; set; }
    public TaskType Type { get; set; }
}
