using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TodoApp.Data;
using TodoApp.Infrastructure.Storage;

namespace TodoApp.Features.Tasks.Attachments.Cleanup;

internal sealed partial class OrphanFileCleaner(
    IDbContextFactory<ApplicationDbContext> factory,
    IFileStorage storage,
    TimeProvider timeProvider,
    IOptions<OrphanFileCleanupOptions> options,
    ILogger<OrphanFileCleaner> logger)
{
    private const int BatchSize = 500;

    /// <summary>
    /// Supprimme les fichiers orphelins
    /// </summary>
    /// <returns>Le nombre de fichiers supprimés.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime - options.Value.GracePeriod;
        var scanned = 0;
        var deleted = 0;
        var batch = new List<string>(BatchSize);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        await foreach (var file in storage.ListAsync(cancellationToken))
        {
            scanned++;

            if (file.LastModifiedUtc >= cutoffUtc)
            {
                continue; // trop récent : peut-être un upload en cours
            }

            batch.Add(file.Key);
            if (batch.Count == BatchSize)
            {
                deleted += await DeleteOrphansAsync(db, batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            deleted += await DeleteOrphansAsync(db, batch, cancellationToken);
        }

        LogCompleted(logger, scanned, deleted);
        return deleted;
    }

    private async Task<int> DeleteOrphansAsync(
        ApplicationDbContext db,
        List<string> keys,
        CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters : une tâche en corbeille peut être restaurée, ses fichiers sont encore référencés.
        var referenced = await db.TaskAttachments
            .IgnoreQueryFilters()
            .TagWith("OrphanFileCleaner : clés référencées")
            .Where(a => keys.Contains(a.StorageKey))
            .Select(a => a.StorageKey)
            .ToListAsync(cancellationToken);

        var referencedKeys = referenced.ToHashSet(StringComparer.Ordinal);
        var deleted = 0;

        foreach (var key in keys.Where(k => !referencedKeys.Contains(k)))
        {
            try
            {
                await storage.DeleteAsync(key, cancellationToken);
                deleted++;
            }
            catch (IOException ex)
            {
                // Un fichier verrouillé ne doit pas arrêter le nettoyage : il sera retenté au prochain passage.
                LogDeleteFailed(logger, ex, key);
            }
        }

        return deleted;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Nettoyage des pièces jointes : {Scanned} fichiers examinés, {Deleted} orphelins supprimés")]
    private static partial void LogCompleted(ILogger logger, int scanned, int deleted);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fichier orphelin {StorageKey} non supprimé, nouvel essai au prochain passage")]
    private static partial void LogDeleteFailed(ILogger logger, Exception exception, string storageKey);
}
