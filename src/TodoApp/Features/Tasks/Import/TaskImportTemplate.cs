using System.Text;

namespace TodoApp.Features.Tasks.Import;

/// <summary>Modèle CSV proposé au téléchargement, aligné sur ce qu'accepte <see cref="TaskCsvParser"/>.</summary>
internal static class TaskImportTemplate
{
    public const string FileName = "modele-import-taches.csv";
    public const string ContentType = "text/csv; charset=utf-8";

    private const string ResourceName = "TaskImportTemplate.csv";

    public static byte[] Create()
    {
        using var stream = typeof(TaskImportTemplate).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Ressource « {ResourceName} » introuvable : vérifie l'EmbeddedResource du .csproj.");

        // Lecture puis réécriture : on maîtrise le BOM et les fins de ligne,
        // même si le fichier a été enregistré sans BOM ou converti en LF par Git.
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var csv = reader.ReadToEnd().ReplaceLineEndings("\r\n");

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(csv)];
    }
}
