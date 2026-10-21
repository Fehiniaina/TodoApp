namespace TodoApp.Features.Tasks.ListTasks;

internal sealed record ListTasksQuery(
    string CreatedById,
    string? Priority = default,
    int Page = 1,
    int PageSize = 10);
