using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Shared;

internal static class TaskQueries
{
    /// <summary>
    /// Tâches que l'utilisateur peut voir et modifier : celles qu'il a créées.
    /// (Étendue à l'assigné en 2.3.)
    /// </summary>
    public static IQueryable<TaskItem> VisibleTo(this IQueryable<TaskItem> tasks, string userId) =>
        tasks.Where(t => t.CreatedById == userId);

    /// <summary>Actions réservées au créateur : corbeille, restauration, purge.</summary>
    public static IQueryable<TaskItem> CreatedBy(this IQueryable<TaskItem> tasks, string userId) =>
        tasks.Where(t => t.CreatedById == userId);
}
