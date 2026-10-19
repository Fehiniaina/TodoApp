namespace TodoApp.Features.Tasks.DeleteTasks;

internal sealed record DeleteTasksCommand(Guid Id, string OwnerId);
