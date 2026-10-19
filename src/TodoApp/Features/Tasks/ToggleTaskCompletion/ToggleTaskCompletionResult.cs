namespace TodoApp.Features.Tasks.ToggleTaskCompletion;

internal sealed record ToggleTaskCompletionResult(Guid Id, bool IsCompleted, DateTime? CompletedAt);