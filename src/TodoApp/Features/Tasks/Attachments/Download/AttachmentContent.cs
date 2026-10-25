namespace TodoApp.Features.Tasks.Attachments.Download;

/// <summary>Le flux doit être fermé par l'appelant (Results.File le fait).</summary>
internal sealed record AttachmentContent(Stream Content, string FileName, string ContentType);
