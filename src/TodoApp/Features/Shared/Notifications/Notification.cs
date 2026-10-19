namespace TodoApp.Features.Shared.Notifications;

public sealed record Notification(Guid Id, NotificationLevel Level, string Message, string? Title);