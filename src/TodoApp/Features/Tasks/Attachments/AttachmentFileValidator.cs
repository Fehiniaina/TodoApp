using System.Collections.Frozen;
using TodoApp.Common;
using TodoApp.Domain.Tasks.Attachments;

namespace TodoApp.Features.Tasks.Attachments;

internal static class AttachmentFileValidator
{
    /// <summary>Nombre d'octets à lire au début du fichier pour la vérification.</summary>
    public const int HeaderLength = 8192;

    private delegate bool SignatureCheck(ReadOnlySpan<byte> header);

    private sealed record FileKind(string ContentType, SignatureCheck Matches);

    private static readonly FrozenDictionary<string, FileKind> AllowedTypes =
        new Dictionary<string, FileKind>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new("application/pdf", h => h.StartsWith("%PDF-"u8)),
            [".png"] = new("image/png", h => h.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])),
            [".jpg"] = new("image/jpeg", IsJpeg),
            [".jpeg"] = new("image/jpeg", IsJpeg),
            [".gif"] = new("image/gif", h => h.StartsWith("GIF87a"u8) || h.StartsWith("GIF89a"u8)),
            [".webp"] = new("image/webp", h => h.Length >= 12 && h.StartsWith("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8)),
            [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", IsZip),
            [".xlsx"] = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", IsZip),
            [".pptx"] = new("application/vnd.openxmlformats-officedocument.presentationml.presentation", IsZip),
            [".zip"] = new("application/zip", IsZip),
            [".txt"] = new("text/plain; charset=utf-8", IsText),
            [".csv"] = new("text/csv; charset=utf-8", IsText),
            [".md"] = new("text/markdown; charset=utf-8", IsText),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> AllowedExtensions => AllowedTypes.Keys;

    private static readonly Error Empty =
        Error.Validation("Attachment.Empty", "Le fichier est vide.");
    private static readonly Error TooLarge =
        Error.Validation("Attachment.TooLarge", $"Le fichier dépasse {TaskAttachment.MaxSizeBytes / (1024 * 1024)} Mo.");
    private static readonly Error TypeNotAllowed =
        Error.Validation("Attachment.TypeNotAllowed", "Ce type de fichier n'est pas autorisé.");
    private static readonly Error ContentMismatch =
        Error.Validation("Attachment.ContentMismatch", "Le contenu du fichier ne correspond pas à son extension.");

    /// <param name="originalName">Nom fourni par le navigateur : non fiable.</param>
    /// <param name="sizeBytes">Taille annoncée ; le handler vérifiera aussi la taille réellement lue.</param>
    /// <param name="header">Les premiers octets du fichier (jusqu'à <see cref="HeaderLength"/>).</param>
    public static Result<ValidatedAttachment> Validate(string? originalName, long sizeBytes, ReadOnlySpan<byte> header)
    {
        if (sizeBytes <= 0 || header.IsEmpty)
        {
            return Empty;
        }

        if (sizeBytes > TaskAttachment.MaxSizeBytes)
        {
            return TooLarge;
        }

        var fileName = AttachmentFileName.Sanitize(originalName);

        if (!AllowedTypes.TryGetValue(Path.GetExtension(fileName), out var kind))
        {
            return TypeNotAllowed;
        }

        if (!kind.Matches(header))
        {
            return ContentMismatch;
        }

        // Le type MIME vient de notre table, jamais de celui annoncé par le navigateur.
        return new ValidatedAttachment(fileName, kind.ContentType);
    }

    private static bool IsJpeg(ReadOnlySpan<byte> header) =>
        header.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]);

    // Formats Office modernes et ZIP : archive standard (« PK\x03\x04 ») ou vide (« PK\x05\x06 »).
    private static bool IsZip(ReadOnlySpan<byte> header) =>
        header.StartsWith((ReadOnlySpan<byte>)[0x50, 0x4B, 0x03, 0x04])
        || header.StartsWith((ReadOnlySpan<byte>)[0x50, 0x4B, 0x05, 0x06]);

    // Texte : aucun octet nul. Un exécutable, une image ou une archive en contiennent toujours.
    private static bool IsText(ReadOnlySpan<byte> header) => !header.Contains((byte)0);
}
