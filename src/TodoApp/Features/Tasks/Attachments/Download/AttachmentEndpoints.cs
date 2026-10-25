using TodoApp.Infrastructure.Http;

namespace TodoApp.Features.Tasks.Attachments.Download;

internal static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tasks/{taskId:guid}/attachments/{attachmentId:guid}", DownloadAsync)
            .RequireAuthorization()
            .WithCurrentUser()
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>Lien vers une pièce jointe, pour l'interface (relatif à la base de l'application).</summary>
    public static string DownloadUrl(Guid taskId, Guid attachmentId) => $"tasks/{taskId}/attachments/{attachmentId}";

    private static async Task<IResult> DownloadAsync(
        Guid taskId,
        Guid attachmentId,
        GetAttachmentContentHandler handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(taskId, attachmentId, cancellationToken);

        // 404 aussi pour « pas d'accès » : on ne révèle pas l'existence d'une pièce jointe.
        if (result.IsFailure)
        {
            return Results.NotFound();
        }

        var headers = http.Response.Headers;
        headers.XContentTypeOptions = "nosniff";                     // le navigateur ne devine pas le type
        headers.ContentSecurityPolicy = "default-src 'none'; sandbox"; // aucun script, même si le fichier était ouvert
        headers.CacheControl = "private, no-store";                  // pas de copie dans un cache partagé

        var file = result.Value;

        // fileDownloadName → Content-Disposition: attachment, avec filename* en UTF-8 pour les accents.
        return Results.File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }
}
