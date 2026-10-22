namespace TodoApp.Domain.Tasks.Checklist;

public sealed record ChecklistItemDto(Guid Id, string Title, bool IsDone);