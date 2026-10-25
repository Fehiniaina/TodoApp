namespace TodoApp.Features.Tasks.Attachments.Delete;

internal sealed record DeleteTaskAttachmentCommand(Guid TaskId, Guid AttachmentId);
