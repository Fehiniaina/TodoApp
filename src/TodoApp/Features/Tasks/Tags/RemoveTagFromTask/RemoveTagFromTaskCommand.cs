namespace TodoApp.Features.Tasks.Tags.RemoveTagFromTask;

internal sealed record RemoveTagFromTaskCommand(Guid TaskId, Guid TagId);
