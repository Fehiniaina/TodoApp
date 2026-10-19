namespace TodoApp.Features.Tasks.ListTasks;

internal sealed record ListTasksQuery(
    string OwnerId,
    string? Priority = default,
    int Page = 1,
    int PageSize = 10);