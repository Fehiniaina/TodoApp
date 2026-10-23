using TodoApp.Domain.Notifications;

namespace TodoApp.Features.Notifications.Shared;

internal sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid TaskId,
    string TaskTitle,
    string? ActorId,
    string ActorName,
    DateTime CreatedAtUtc,
    bool IsRead);
