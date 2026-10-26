using Microsoft.Extensions.Options;

namespace TodoApp.Features.Tasks.Attachments.Cleanup;

internal sealed partial class OrphanFileCleanupService(
    OrphanFileCleaner cleaner,
    IOptions<OrphanFileCleanupOptions> options,
    ILogger<OrphanFileCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);

        do
        {
            try
            {
                await cleaner.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogCycleFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Nettoyage des pièces jointes orphelines en échec")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
