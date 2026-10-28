namespace TodoApp.Features.Tasks.ListTasks;

/// <summary>Option du filtre « Assigné ». Découplé du DTO du handler.</summary>
internal sealed record AssigneeFilterOption(string Id, string DisplayName);
