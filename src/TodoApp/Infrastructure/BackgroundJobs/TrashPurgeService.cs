using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;

namespace TodoApp.Infrastructure.BackgroundJobs;

internal sealed class TrashPurgeService(
    IDbContextFactory<ApplicationDbContext> factory,
    TimeProvider timeProvider,
    ILogger<TrashPurgeService> logger) : BackgroundService
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        do
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(stoppingToken);
                var threshold = timeProvider.GetUtcNow().UtcDateTime - Retention;

                var purged = await db.Tasks
                    .IgnoreQueryFilters([QueryFilters.SoftDelete])
                    .Where(t => t.IsDeleted && t.DeletedAt < threshold)
                    .ExecuteDeleteAsync(stoppingToken);

                if (purged > 0)
                {
                    logger.LogInformation("Purged {Count} task(s) older than {RetentionDays} days from trash",
                        purged, Retention.TotalDays);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Une purge ratée ne doit pas arrêter le service : on réessaiera au prochain tick.
                logger.LogError(ex, "Trash purge failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
