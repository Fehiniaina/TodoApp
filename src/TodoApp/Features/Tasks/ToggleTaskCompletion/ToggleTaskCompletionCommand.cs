namespace TodoApp.Features.Tasks.ToggleTaskCompletion;

internal sealed record ToggleTaskCompletionCommand(Guid Id, bool IsCompleted);