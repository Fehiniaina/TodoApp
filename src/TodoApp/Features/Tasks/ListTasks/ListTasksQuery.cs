namespace TodoApp.Features.Tasks.ListTasks;

internal sealed record ListTasksQuery(TaskScope Scope, int Page, int PageSize);
