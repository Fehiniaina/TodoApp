using System.Collections.Frozen;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.Features.Tasks.Import;

/// <summary>Lit un CSV de tâches. Aucune dépendance à la base : testable en isolation.</summary>
internal static class TaskCsvParser
{
    public const int MaxRows = 500;
    public const long MaxFileSize = 2 * 1024 * 1024;

    private const char ReplacementChar = '\uFFFD'; // caractère produit par un mauvais encodage

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private static readonly ImportedTaskFieldsValidator FieldsValidator = new();

    private static readonly (string Key, TaskPriority Value)[] PrioritySynonyms =
    [
        ("NORMALE", TaskPriority.Medium),
    ];

    private static readonly FrozenDictionary<string, TaskPriority> Priorities =
    Enum.GetValues<TaskPriority>()
        .SelectMany(p => new[] { (Key: Normalize(p.ToString()), Value: p), (Key: Normalize(p.ToLabel()), Value: p) })
        .Concat(PrioritySynonyms)
        .DistinctBy(x => x.Key, StringComparer.Ordinal)
        .ToFrozenDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, Column> HeaderAliases =
        new Dictionary<string, Column>(StringComparer.Ordinal)
        {
            ["TITRE"] = Column.Title,
            ["TITLE"] = Column.Title,
            ["DESCRIPTION"] = Column.Description,
            ["PRIORITE"] = Column.Priority,
            ["PRIORITY"] = Column.Priority,
            ["ECHEANCE"] = Column.DueDate,
            ["DATE D'ECHEANCE"] = Column.DueDate,
            ["DUE DATE"] = Column.DueDate,
            ["DUEDATE"] = Column.DueDate,
            ["ASSIGNE"] = Column.Assignee,
            ["ASSIGNEE"] = Column.Assignee,
            ["EMAIL ASSIGNE"] = Column.Assignee,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, TaskType> Types =
        Enum.GetValues<TaskType>()
        .SelectMany(t => new[] { (Key: Normalize(t.ToString()), Type: t), (Key: Normalize(t.ToLabel()), Type: t) })
        .DistinctBy(x => x.Key, StringComparer.Ordinal)
        .ToFrozenDictionary(x => x.Key, x => x.Type, StringComparer.Ordinal);

    public static async Task<TaskCsvParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        // detectEncodingFromByteOrderMarks : accepte l'UTF-8 avec BOM (Excel « CSV UTF-8 »).
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            DetectDelimiterValues = [";", ","],
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            MissingFieldFound = null, // une ligne plus courte que l'en-tête : colonnes vides
        };

        using var csv = new CsvReader(reader, config);

        try
        {
            if (!await csv.ReadAsync() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                return FileError("Le fichier est vide.");
            }

            var columns = MapHeader(csv.HeaderRecord);
            if (!columns.ContainsKey(Column.Title))
            {
                return FileError("La colonne « Titre » est obligatoire. Utilisez le modèle fourni.");
            }

            var rows = new List<ParsedTaskRow>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (rows.Count == MaxRows)
                {
                    return FileError($"Le fichier dépasse {MaxRows} lignes : découpez-le en plusieurs fichiers.");
                }

                rows.Add(ParseRow(csv, columns));
            }

            return rows.Count == 0
                ? FileError("Le fichier ne contient aucune tâche.")
                : new TaskCsvParseResult(rows, []);
        }
        catch (CsvHelperException ex)
        {
            var line = ex.Context?.Parser?.RawRow;
            return FileError(line is null
                ? "Le fichier CSV est mal formé."
                : $"Le fichier CSV est mal formé (ligne {line}).");
        }
    }

    private static ParsedTaskRow ParseRow(CsvReader csv, Dictionary<Column, int> columns)
    {
        var errors = new List<string>();

        string? Field(Column column) =>
            columns.TryGetValue(column, out var index) && csv.GetField(index) is { Length: > 0 } value
                ? value
                : null;

        var title = Field(Column.Title) ?? string.Empty;
        var description = Field(Column.Description);

        if (title.Contains(ReplacementChar, StringComparison.Ordinal)
            || description?.Contains(ReplacementChar, StringComparison.Ordinal) == true)
        {
            errors.Add("Caractères illisibles : enregistrez le fichier au format « CSV UTF-8 ».");
        }

        var fieldsValidation = FieldsValidator.Validate(new ImportedTaskFields(title, description));
        errors.AddRange(fieldsValidation.Errors.Select(e => e.ErrorMessage));

        var priority = TaskPriority.Medium;
        if (Field(Column.Priority) is { } priorityText
            && !Priorities.TryGetValue(Normalize(priorityText), out priority))
        {
            errors.Add($"Priorité « {priorityText} » inconnue (attendu : Basse, Moyenne, Haute ou Urgente).");
        }

        var type = TaskType.Task;
        if (Field(Column.Type) is  { } typeText
            && !Types.TryGetValue(Normalize(typeText), out type))
        {
            errors.Add($"Type de tache « {typeText} » inconnue (attendu : Bug, Extraction, Analyse, Task ou Story).");
        }


        DateOnly? dueDate = null;
        if (Field(Column.DueDate) is { } dueText)
        {
            if (DateOnly.TryParseExact(dueText, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                dueDate = parsed;
            }
            else
            {
                errors.Add($"Échéance « {dueText} » invalide (format attendu : jj/mm/aaaa).");
            }
        }

        var assigneeEmail = Field(Column.Assignee);
        if (assigneeEmail is not null && !MailAddress.TryCreate(assigneeEmail, out _))
        {
            errors.Add($"Adresse « {assigneeEmail} » invalide.");
        }

        return new ParsedTaskRow(csv.Parser.RawRow, title, description, priority, dueDate, assigneeEmail, type, errors);
    }

    private static Dictionary<Column, int> MapHeader(string[] header)
    {
        var columns = new Dictionary<Column, int>();

        for (var i = 0; i < header.Length; i++)
        {
            if (HeaderAliases.TryGetValue(Normalize(header[i]), out var column))
            {
                columns.TryAdd(column, i); // en cas de doublon, la première colonne l'emporte
            }
        }

        return columns;
    }

    /// <summary>« Échéance » → « ECHEANCE » : insensible à la casse, aux accents et aux espaces.</summary>
    internal static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static TaskCsvParseResult FileError(string message) => new([], [message]);
}
