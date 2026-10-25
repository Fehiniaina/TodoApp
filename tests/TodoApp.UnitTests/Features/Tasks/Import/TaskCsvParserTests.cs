using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Import;

namespace TodoApp.UnitTests.Features.Tasks.Import;

public sealed class TaskCsvParserTests
{
    [Fact]
    public async Task Parses_semicolon_file_with_french_headers_and_bom()
    {
        var result = await ParseAsync(
            "Titre;Description;Priorité;Échéance;Assigné\n" +
            "Préparer la démo;Slides;Haute;15/11/2026;karim@exemple.com\n",
            withBom: true);

        Assert.Empty(result.FileErrors);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Préparer la démo", row.Title);
        Assert.Equal("Slides", row.Description);
        Assert.Equal(TaskPriority.High, row.Priority);
        Assert.Equal(new DateOnly(2026, 11, 15), row.DueDate);
        Assert.Equal("karim@exemple.com", row.AssigneeEmail);
        Assert.Empty(row.Errors);
    }

    [Fact]
    public async Task Parses_comma_file_with_english_headers()
    {
        var result = await ParseAsync("Title,Priority,Due date\nFix bug,urgent,2026-11-30\n");

        var row = Assert.Single(result.Rows);
        Assert.Equal(TaskPriority.Urgent, row.Priority);
        Assert.Equal(new DateOnly(2026, 11, 30), row.DueDate);
    }

    [Fact]
    public async Task Missing_priority_defaults_to_medium()
    {
        var result = await ParseAsync("Titre\nTâche simple\n");

        Assert.Equal(TaskPriority.Medium, Assert.Single(result.Rows).Priority);
    }

    [Fact]
    public async Task Quoted_fields_keep_separators_and_line_breaks()
    {
        var result = await ParseAsync(
            "Titre;Description\n" +
            "\"Export ; spécial\";\"Ligne 1\nLigne 2\"\n" +
            "Suivante;\n");

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("Export ; spécial", result.Rows[0].Title);
        Assert.Equal("Ligne 1\nLigne 2", result.Rows[0].Description);
        Assert.Equal(4, result.Rows[1].LineNumber); // la description occupait les lignes 2 et 3
    }

    [Theory]
    [InlineData("Titre;Priorité\nTâche;Hte\n", "Priorité « Hte » inconnue")]
    [InlineData("Titre;Échéance\nTâche;31/02/2026\n", "Échéance « 31/02/2026 » invalide")]
    [InlineData("Titre;Assigné\nTâche;pas-un-email\n", "Adresse « pas-un-email » invalide")]
    public async Task Invalid_values_produce_row_errors(string csv, string expectedError)
    {
        var result = await ParseAsync(csv);

        var row = Assert.Single(result.Rows);
        Assert.Contains(row.Errors, e => e.StartsWith(expectedError, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Empty_title_is_a_row_error()
    {
        var result = await ParseAsync("Titre;Description\n;Sans titre\n");

        Assert.NotEmpty(Assert.Single(result.Rows).Errors);
    }

    [Fact]
    public async Task Missing_title_column_is_a_file_error()
    {
        var result = await ParseAsync("Description;Priorité\nQuelque chose;Haute\n");

        Assert.Empty(result.Rows);
        Assert.Contains("Titre", Assert.Single(result.FileErrors), StringComparison.Ordinal);
    }

    [Fact]
    public async Task File_over_the_row_limit_is_rejected()
    {
        var csv = new StringBuilder("Titre\n");
        for (var i = 0; i <= TaskCsvParser.MaxRows; i++)
        {
            csv.Append("Tâche ").Append(i).Append('\n');
        }

        var result = await ParseAsync(csv.ToString());

        Assert.Empty(result.Rows);
        Assert.Single(result.FileErrors);
    }

    [Fact]
    public async Task Windows_1252_file_is_detected_as_badly_encoded()
    {
        var bytes = Encoding.Latin1.GetBytes("Titre\nPréparer la démo\n");
        using var stream = new MemoryStream(bytes);

        var result = await TaskCsvParser.ParseAsync(stream, CancellationToken.None);

        Assert.Contains(Assert.Single(result.Rows).Errors, e => e.Contains("CSV UTF-8", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Échéance", "ECHEANCE")]
    [InlineData("  priorité ", "PRIORITE")]
    [InlineData("Assigné", "ASSIGNE")]
    public void Normalize_ignores_case_accents_and_spaces(string input, string expected)
    {
        Assert.Equal(expected, TaskCsvParser.Normalize(input));
    }

    private static async Task<TaskCsvParseResult> ParseAsync(string csv, bool withBom = false)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: withBom);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(csv)).ToArray();
        using var stream = new MemoryStream(bytes);

        return await TaskCsvParser.ParseAsync(stream, CancellationToken.None);
    }
}
