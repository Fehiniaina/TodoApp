namespace TodoApp.Features.Tasks.AssignTask;

/// <summary>
/// <param name="AssigneeId">null pour retirer l'assignation.</param>
/// </summary>
internal sealed record AssignTaskCommand(Guid TaskId, string? AssigneeId);
