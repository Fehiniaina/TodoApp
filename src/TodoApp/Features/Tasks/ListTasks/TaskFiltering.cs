using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.ListTasks;

internal static class TaskFiltering
{
    public static IQueryable<TaskItem> ApplyFilter(this IQueryable<TaskItem> tasks, TaskFilter filter, DateOnly today)
    {
        if (filter.Priorities.Count > 0)
        {
            // Liste plutôt que FrozenSet : EF traduit List.Contains en « = ANY(@p) ».
            var priorities = filter.Priorities.ToList();
            tasks = tasks.Where(t => priorities.Contains(t.Priority));
        }

        if (filter.FiltersAssignee)
        {
            var assigneeIds = filter.AssigneeIds.ToList();
            var includeUnassigned = filter.IncludeUnassigned;

            tasks = tasks.Where(t =>
                (t.AssigneeId != null && assigneeIds.Contains(t.AssigneeId))
                || (includeUnassigned && t.AssigneeId == null));
        }

        return filter.DueDate switch
        {
            DueDateFilter.Overdue => tasks.Where(t => t.DueDate < today && t.CompletedAt == null),
            DueDateFilter.Today => tasks.Where(t => t.DueDate == today),
            DueDateFilter.Next7Days => WithinRange(tasks, today, today.AddDays(6)),
            DueDateFilter.NoDueDate => tasks.Where(t => t.DueDate == null),
            DueDateFilter.Range => WithinRange(tasks, filter.DueFrom, filter.DueTo),
            _ => tasks,
        };
    }

    private static IQueryable<TaskItem> WithinRange(IQueryable<TaskItem> tasks, DateOnly? from, DateOnly? to)
    {
        if (from is { } start)
        {
            tasks = tasks.Where(t => t.DueDate >= start);
        }

        if (to is { } end)
        {
            tasks = tasks.Where(t => t.DueDate <= end);
        }

        return tasks;
    }
}
