using Microsoft.Extensions.Options;

namespace TodoApp.Features.Notifications.Email;

internal sealed partial class EmailDeliveryService(
    EmailDeliveryProcessor processor,
    EmailDeliverySignal signal,
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.PollingInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await processor.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogCycleFailed(logger, ex);
            }

            // Réveil immédiat sur nouvelle notification ; sinon cycle normal (nouvelles tentatives, redémarrage).
            await signal.WaitAsync(interval, stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle d'envoi des emails en échec")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
