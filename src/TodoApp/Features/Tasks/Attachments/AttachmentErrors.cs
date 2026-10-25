using TodoApp.Common;
using TodoApp.Domain.Tasks;

namespace TodoApp.Features.Tasks.Attachments;

internal static class AttachmentErrors
{
    public static readonly Error TaskNotFound =
        Error.NotFound("Task.NotFound", "Cette tâche n'existe pas ou a été supprimée.");

    public static readonly Error NotFound =
        Error.NotFound("Attachment.NotFound", "Cette pièce jointe n'existe plus.");

    public static readonly Error LimitReached =
        Error.Validation("Attachment.LimitReached", $"Une tâche ne peut pas avoir plus de {TaskItem.MaxAttachments} pièces jointes.");

    public static readonly Error CannotDelete =
        Error.Forbidden("Attachment.CannotDelete", "Seul l'auteur de la pièce jointe ou le créateur de la tâche peut la supprimer.");
}
