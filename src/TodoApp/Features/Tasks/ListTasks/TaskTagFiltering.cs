using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

internal static class TaskTagFiltering
{
    /// <summary>Tâches portant AU MOINS UN des tags demandés (OU entre les tags, comme les autres critères).</summary>
    public static IQueryable<TaskItem> WithAnyTag(this IQueryable<TaskItem> tasks, IReadOnlyCollection<Guid> tagIds)
    {
        var ids = tagIds.ToArray();
        return tasks.SelectMany(
            t => t.Tags.Where(tt => ids.Contains(tt.TagId)),
            (t, _) => t);
    }
}
