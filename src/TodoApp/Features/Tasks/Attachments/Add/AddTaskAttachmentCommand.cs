namespace TodoApp.Features.Tasks.Attachments.Add;

/// <param name="FileName">Nom fourni par le navigateur : non fiable, il sera nettoyé.</param>
/// <param name="Content">Flux du fichier. Le handler ne le ferme pas : c'est à l'appelant de le faire.</param>
internal sealed record AddTaskAttachmentCommand(Guid TaskId, string FileName, Stream Content);
