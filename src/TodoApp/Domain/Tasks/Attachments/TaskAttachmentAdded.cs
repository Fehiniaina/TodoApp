using TodoApp.Domain.Common;

namespace TodoApp.Domain.Tasks.Attachments;

internal sealed record TaskAttachmentAdded(
    Guid TaskId,
    string ActorId,
    Guid AttachmentId,
    string FileName,
    DateTime OccurredAtUtc) : IDomainEvent;
