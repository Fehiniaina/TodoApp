namespace TodoApp.Features.Tasks.Attachments.List;

internal sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc,
    string? UploadedById,
    string UploadedByName);
