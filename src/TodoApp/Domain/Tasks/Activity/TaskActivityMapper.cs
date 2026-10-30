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
            e.TaskId, e.ActorId, TaskActivityType.StatusChanged, EnumStorage.ToStored(e.From), EnumStorage.ToStored(e.To), e.OccurredAtUtc),

        TaskAttachmentAdded e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.AttachmentAdded, oldValue: null, newValue: e.FileName, e.OccurredAtUtc),

        TaskAttachmentRemoved e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.AttachmentRemoved, oldValue: e.FileName, newValue: null, e.OccurredAtUtc),

        TaskTypeChanged e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.TypeChanged, oldValue: EnumStorage.ToStored(e.Previous), newValue: EnumStorage.ToStored(e.Current), e.OccurredAtUtc),

        TaskTagAdded e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.TagAdded, oldValue: null, newValue: e.TagName, e.OccurredAtUtc),

        TaskTagRemoved e => TaskActivity.Record(
            e.TaskId, e.ActorId, TaskActivityType.TagRemoved, oldValue: e.TagName, newValue: null, e.OccurredAtUtc),

        _ => null,
    };
}
