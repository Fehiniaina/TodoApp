using TodoApp.Domain.Common;
using TodoApp.Domain.Tasks.Attachments;
using TodoApp.Domain.Tasks.Events;

namespace TodoApp.Domain.Tasks.Activity;

internal static class TaskActivityMapper
{
    /// <summary>null : événement qui ne produit pas d'historique.</summary>
    public static TaskActivity? ToActivity(IDomainEvent domainEvent) => domainEvent switch
    {
        TaskCreated e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.Created, null, null, e.OccurredAtUtc),

        TaskAssigned e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.Assigned, e.PreviousAssigneeId, e.NewAssigneeId, e.OccurredAtUtc),

        TaskStatusChanged e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.StatusChanged, e.From.ToString(), e.To.ToString(), e.OccurredAtUtc),

        TaskAttachmentAdded e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.AttachmentAdded, oldValue: null, newValue: e.FileName, e.OccurredAtUtc),

        TaskAttachmentRemoved e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.AttachmentRemoved, oldValue: e.FileName, newValue: null, e.OccurredAtUtc),

        TaskTypeChanged e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.TypeChanged, oldValue: e.Previous.ToString(), newValue: e.Current.ToString(),e.OccurredAtUtc),

        _ => null,
    };
}
