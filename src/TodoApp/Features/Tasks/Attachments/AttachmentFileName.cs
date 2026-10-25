using System.Buffers;
using System.Globalization;
using System.Text;
using TodoApp.Domain.Tasks.Attachments;

namespace TodoApp.Features.Tasks.Attachments;

/// <summary>
/// Nettoie le nom d'origine. Il ne sert qu'à l'affichage et au téléchargement :
/// le fichier est stocké sous une clé GUID, jamais sous ce nom.
/// </summary>
internal static class AttachmentFileName
{
    private const string Fallback = "fichier";

    private static readonly SearchValues<char> Forbidden = SearchValues.Create("<>:\"/\\|?*");

    public static string Sanitize(string? originalName)
    {
        var name = originalName ?? string.Empty;

        var lastSeparator = name.LastIndexOfAny(['/', '\\']);
        if (lastSeparator >= 0)
        {
            name = name[(lastSeparator + 1)..];
        }

        // 2. Retire les caractères de contrôle, de format et interdits.
        var builder = new StringBuilder(name.Length);
        foreach (var c in name.Normalize(NormalizationForm.FormC))
        {
            if (char.IsControl(c)
                || char.GetUnicodeCategory(c) == UnicodeCategory.Format
                || Forbidden.Contains(c))
            {
                continue;
            }

            builder.Append(char.IsWhiteSpace(c) ? ' ' : c);
        }

        // 3. Espaces multiples réduits ; points et espaces finaux retirés (invalides sous Windows).
        var cleaned = string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.', ' ');

        // 4. Nom de base obligatoire et longueur maximale, en conservant l'extension.
        var extension = Path.GetExtension(cleaned);
        var baseName = Path.GetFileNameWithoutExtension(cleaned).Trim();

        if (baseName.Length == 0)
        {
            baseName = Fallback;
        }

        var maxBaseLength = TaskAttachment.MaxFileNameLength - extension.Length;
        if (baseName.Length > maxBaseLength)
        {
            baseName = baseName[..maxBaseLength].TrimEnd();
        }

        return baseName + extension;
    }
}
