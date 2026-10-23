using Microsoft.Extensions.Options;

namespace TodoApp.Features.Notifications.Email;

internal sealed partial class EmailDeliveryService(
    EmailDeliveryProcessor processor,
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval);

        do
        {
            try
            {
                await processor.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Base indisponible, etc. : on réessaie au cycle suivant sans arrêter le service.
                LogCycleFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle d'envoi des emails en échec")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
