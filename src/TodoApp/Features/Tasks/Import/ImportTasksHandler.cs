using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;
using TodoApp.Domain.Tasks;
using TodoApp.Features.Shared;

namespace TodoApp.Features.Tasks.Import;

internal sealed partial class ImportTasksHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TaskChangeNotifier notifier,
    ILogger<ImportTasksHandler> logger) : IHandler
{
    public async Task<Result<int>> HandleAsync(ImportTasksCommand command, CancellationToken cancellationToken = default)
    {
        var rows = command.Rows;

        if (rows.Count is 0 or > TaskCsvParser.MaxRows)
        {
            return Error.Validation("Import.InvalidSize", $"L'import doit contenir entre 1 et {TaskCsvParser.MaxRows} tâches.");
        }

        if (rows.Any(r => !r.IsValid))
        {
            return Error.Validation("Import.InvalidRows", "Corrigez les lignes en erreur avant d'importer.");
        }

        var userId = await currentUser.GetIdAsync();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // Un assigné a pu être supprimé entre l'aperçu et la confirmation.
        if (!await AllAssigneesExistAsync(db, rows, cancellationToken))
        {
            return Error.Validation("Import.UnknownAssignee", "Un des utilisateurs assignés n'existe plus. Rechargez le fichier.");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            // La liste est triée par date de création décroissante : en décalant d'une milliseconde,
            // la première ligne du fichier apparaît en tête de liste, dans l'ordre du CSV.
            var createdAtUtc = nowUtc.AddMilliseconds(-i);

            var task = TaskItem.Create(userId, row.Title, row.Description, row.Priority, row.DueDate, createdAtUtc);

            if (row.AssigneeId is not null)
            {
                task.AssignTo(row.AssigneeId, userId, createdAtUtc, AssignmentOrigin.Import);
            }

            db.Tasks.Add(task);
        }

        // Un seul SaveChanges = une seule transaction : toutes les tâches, ou aucune.
        await db.SaveChangesAsync(cancellationToken);

        notifier.NotifyChanged();
        LogImported(logger, rows.Count);

        return rows.Count;
    }

    private static async Task<bool> AllAssigneesExistAsync(
        ApplicationDbContext db,
        IReadOnlyList<TaskImportRow> rows,
        CancellationToken cancellationToken)
    {
        var ids = rows
            .Select(r => r.AssigneeId)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return true;
        }

        var existing = await db.Users.CountAsync(u => ids.Contains(u.Id), cancellationToken);
        return existing == ids.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} tâches importées depuis un CSV")]
    private static partial void LogImported(ILogger logger, int count);
}
