using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TodoApp.Common;
using TodoApp.Data;

namespace TodoApp.Infrastructure.BackgroundJobs;

internal sealed partial class TrashPurgeService(
    IDbContextFactory<ApplicationDbContext> factory,
    TimeProvider timeProvider,
    ILogger<TrashPurgeService> logger,
    IOptions<TrashPurgeOptions> options) : BackgroundService
{
    private readonly TimeSpan Retention = options.Value.Retention;
    private readonly TimeSpan Interval = options.Value.Interval;

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
                    LogPurged(purged, threshold);
                }
                else
                {
                    LogNothingToPurge(threshold);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Une purge ratée ne doit pas arrêter le service : on réessaiera au prochain tick.
                LogOnError(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Trash purge started (retention {Retention}, interval {Interval})")]
    private partial void LogStarted(TimeSpan retention, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} task(s) deleted before {Threshold:u}")]
    private partial void LogPurged(int count, DateTime threshold);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Trash purge ran: nothing deleted before {Threshold:u}")]
    private partial void LogNothingToPurge(DateTime threshold);

    [LoggerMessage(Level = LogLevel.Error, Message = "Trash purge failed")]
    private partial void LogOnError(Exception ex);
}
