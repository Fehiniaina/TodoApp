namespace TodoApp.Domain.Notifications;

internal enum EmailDeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Expired,
}
