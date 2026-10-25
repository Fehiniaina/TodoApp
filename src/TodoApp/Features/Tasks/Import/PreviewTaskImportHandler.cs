using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Features.Users;

namespace TodoApp.Features.Tasks.Import;

internal sealed class PreviewTaskImportHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ILookupNormalizer normalizer) : IHandler
{
    public async Task<TaskImportPreview> HandleAsync(Stream csv, CancellationToken cancellationToken = default)
    {
        var parsed = await TaskCsvParser.ParseAsync(csv, cancellationToken);
        if (parsed.FileErrors.Count > 0)
        {
            return new TaskImportPreview([], parsed.FileErrors);
        }

        var assignees = await LoadAssigneesAsync(parsed.Rows, cancellationToken);

        return new TaskImportPreview(
            parsed.Rows.Select(row => Resolve(row, assignees)).ToList(),
            []);
    }

    private TaskImportRow Resolve(
        ParsedTaskRow row,
        Dictionary<string, (string Id, string Name)> assignees)
    {
        string? assigneeId = null;
        string? assigneeName = null;
        var errors = row.Errors;

        if (row.AssigneeEmail is not null)
        {
            if (assignees.TryGetValue(normalizer.NormalizeEmail(row.AssigneeEmail), out var assignee))
            {
                (assigneeId, assigneeName) = assignee;
            }
            else if (row.Errors.Count == 0 || !row.Errors.Any(e => e.Contains(row.AssigneeEmail, StringComparison.Ordinal)))
            {
                errors = [.. row.Errors, $"Aucun utilisateur avec l'adresse « {row.AssigneeEmail} »."];
            }
        }

        return new TaskImportRow(
            row.LineNumber,
            row.Title,
            row.Description,
            row.Priority,
            row.DueDate,
            row.AssigneeEmail,
            assigneeId,
            assigneeName,
            errors);
    }

    private async Task<Dictionary<string, (string Id, string Name)>> LoadAssigneesAsync(
        IReadOnlyList<ParsedTaskRow> rows,
        CancellationToken cancellationToken)
    {
        var normalizedEmails = rows
            .Select(r => r.AssigneeEmail)
            .OfType<string>()
            .Select(normalizer.NormalizeEmail)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedEmails.Count == 0)
        {
            return new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var users = await db.Users
            .TagWith("PreviewTaskImport : assignés")
            .Where(u => u.NormalizedEmail != null && normalizedEmails.Contains(u.NormalizedEmail))
            .Select(u => new { u.Id, u.NormalizedEmail, u.FirstName, u.LastName, u.Email })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.NormalizedEmail!,
            u => (u.Id, UserSummary.ToDisplayName(u.FirstName, u.LastName, u.Email)),
            StringComparer.Ordinal);
    }
}
